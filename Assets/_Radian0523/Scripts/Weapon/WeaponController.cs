using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Velora.Core;
using Velora.Data;
using Velora.Player;

namespace Velora.Weapon
{
    /// <summary>
    /// 射撃・リロード・ADS・武器切替を統合管理する MonoBehaviour。
    /// 各武器の射撃方式は IFireStrategy で差し替え可能（ストラテジーパターン）。
    /// マズルフラッシュ・発射キックなどの視覚演出は WeaponModelView に委譲することで、
    /// Controller は入力とゲームロジックのみを担当する（単一責務の原則）。
    ///
    /// 武器モデルは WeaponData.ModelPrefab から生成し、_modelRegistry で管理する。
    /// 初期武器は _initialWeapons（Inspector 設定）から Start 時に登録。
    /// ランタイムでは AddWeapon() でピックアップ経由の武器追加に対応する。
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("武器データ（初期装備）")]
        [SerializeField] private WeaponData[] _initialWeapons;

        // スロットインデックスに対応した固定長配列。
        // WeaponData.SlotIndex で決まる位置に武器を格納し、
        // キー入力（1-5）と配列インデックスを直接対応させる。
        private const int SlotCount = 5;
        private readonly WeaponData[] _weaponSlots = new WeaponData[SlotCount];

        [Header("リザーブ弾薬（タイプ別初期値）")]
        [SerializeField] private int _initialLightReserve = 120;
        [SerializeField] private int _initialEnergyReserve = 24;
        [SerializeField] private int _initialExplosiveReserve = 5;

        [Header("参照")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Camera _weaponCamera;
        [SerializeField] private FPSController _fpsController;
        [SerializeField] private LayerMask _hitMask;

        [Header("ADS")]
        [SerializeField] private float _defaultFieldOfView = 60f;
        [SerializeField] private float _adsFovLerpSpeed = 10f;

        private WeaponData _currentWeaponData;
        private IFireStrategy _fireStrategy;
        private int _currentWeaponIndex;
        private float _lastFireTime;
        private bool _isReloading;
        private bool _isAiming;
        private bool _isFireHeld;
        private bool _isSwitching;
        private float _lastScrollTime;
        private bool _isDestroying;

        // PlayerModel への参照は、firerateアップなどで必要になるため保持する。
        private PlayerModel _playerModel;

        // 武器モデル管理: WeaponData → 生成済み WeaponModelView のマッピング。
        // Start 時に全武器分を生成し、切替時は SetActive で表示を切り替える。
        private readonly Dictionary<WeaponData, WeaponModelView> _modelRegistry = new();
        private WeaponModelView _activeModelView;

        private WeaponAmmoManager _ammoManager;
        private WeaponEffectPoolManager _effectPoolManager;

        // 武器切替演出パラメータ
        private const float SwitchSlideOffset = -0.4f;
        private const float SwitchOutDuration = 0.15f;
        private const float SwitchInDuration = 0.2f;

        private CancellationTokenSource _reloadCts;

        // UI Presenter が購読するイベント
        public event Action<int, int> OnAmmoChanged;
        public event Action<bool> OnReloadStateChanged;
        public event Action<WeaponData> OnWeaponSwitched;
        public event Action<WeaponData> OnWeaponAdded;
        public event Action<bool> OnAimStateChanged;
        public event Action OnFired;

        public void Initialize(PlayerModel playerModel)
        {
            _playerModel = playerModel;
        }
        public WeaponData CurrentWeaponData => _currentWeaponData;
        public IReadOnlyList<WeaponData> Weapons => _weaponSlots;
        public int CurrentWeaponIndex => _currentWeaponIndex;
        public bool IsAiming => _isAiming;
        public int CurrentAmmo => _ammoManager.CurrentAmmo;
        public int ReserveAmmo => _ammoManager.ReserveAmmo;

        /// <summary>
        /// 初期武器の登録を Awake で行い、他スクリプトの Start() より先に
        /// _weaponSlots を確定させる。BattleSceneDirector.Start() → HudPresenter.Initialize()
        /// が Weapons を参照する時点で武器スロットが空にならないことを保証する。
        /// </summary>
        private void Awake()
        {
            var initialReserves = new Dictionary<AmmoType, int>
            {
                { AmmoType.Light, _initialLightReserve },
                { AmmoType.Energy, _initialEnergyReserve },
                { AmmoType.Explosive, _initialExplosiveReserve }
            };
            _ammoManager = new WeaponAmmoManager(initialReserves);
            _effectPoolManager = new WeaponEffectPoolManager();

            if (_initialWeapons == null || _initialWeapons.Length == 0) return;

            foreach (var weaponData in _initialWeapons)
            {
                if (weaponData == null) continue;
                RegisterWeapon(weaponData);
            }
        }

        private void Start()
        {
            int first = FindFirstOccupiedSlot();
            if (first >= 0) EquipWeapon(first);
        }

        private void Update()
        {
            if (_currentWeaponData == null) return;

            // UI 操作中（カーソルアンロック時）は武器操作を停止し、
            // 保持中の入力状態をリセットする。アップグレード選択やリザルト画面の
            // ボタンクリックで弾が発射されるのを防ぐ。
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _isFireHeld = false;
                _isAiming = false;
                // UI 表示中は武器カメラを無効化し、EventSystem との干渉を防ぐ。
                // URP Camera Stack に Overlay Camera が active で残っていると、
                // GraphicRaycaster のポインタ判定に影響する場合がある。
                SetWeaponCameraEnabled(false);
                return;
            }

            SetWeaponCameraEnabled(true);
            TryAutoFire();
            UpdateAdsFieldOfView();
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            CancelReload();
            _effectPoolManager.Cleanup();

            foreach (var kvp in _modelRegistry)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            _modelRegistry.Clear();
        }

        // --- Input System コールバック ---
        // PlayerInput の Broadcast Messages から呼ばれる（InputValue シグネチャ）

        public void OnFire(InputValue value)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _isFireHeld = value.isPressed;
            if (_isFireHeld)
            {
                TryFire();
            }
        }

        public void OnAim(InputValue value)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            _isAiming = value.isPressed;
            OnAimStateChanged?.Invoke(_isAiming);
        }

        public void OnReload(InputValue value)
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            if (value.isPressed)
            {
                StartReload().Forget();
            }
        }

        public void OnWeaponScroll(InputValue value)
        {
            float scroll = value.Get<float>();
            if (scroll == 0f) return;

            // macOS の慣性スクロールやバウンスで符号が反転するイベントが
            // 連続発生するため、クールダウンで誤切替を防ぐ
            const float scrollCooldown = 0.2f;
            if (Time.unscaledTime - _lastScrollTime < scrollCooldown) return;
            _lastScrollTime = Time.unscaledTime;

            // 空スロットをスキップして次の武器を見つける
            int direction = scroll > 0f ? -1 : 1;
            int nextSlot = FindNextOccupiedSlot(_currentWeaponIndex, direction);
            if (nextSlot >= 0) EquipWeapon(nextSlot);
        }

        public void OnWeapon1(InputValue value) { if (value.isPressed) EquipWeapon(0); }
        public void OnWeapon2(InputValue value) { if (value.isPressed) EquipWeapon(1); }
        public void OnWeapon3(InputValue value) { if (value.isPressed) EquipWeapon(2); }
        public void OnWeapon4(InputValue value) { if (value.isPressed) EquipWeapon(3); }
        public void OnWeapon5(InputValue value) { if (value.isPressed) EquipWeapon(4); }
        public void OnWeapon6(InputValue value) { if (value.isPressed) EquipWeapon(5); }

        // --- スロット検索 ---

        /// <summary>
        /// 現在のスロットから指定方向に巡回し、次に武器が入っているスロットを返す。
        /// 全スロットが空（自分以外）の場合は -1 を返す。
        /// </summary>
        private int FindNextOccupiedSlot(int currentSlot, int direction)
        {
            for (int i = 1; i < SlotCount; i++)
            {
                int candidate = (currentSlot + direction * i + SlotCount) % SlotCount;
                if (_weaponSlots[candidate] != null) return candidate;
            }
            return -1;
        }

        private int FindFirstOccupiedSlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_weaponSlots[i] != null) return i;
            }
            return -1;
        }

        // --- 武器登録・追加 ---

        /// <summary>
        /// 武器モデルとその子オブジェクト全体のレイヤーを一括設定する。
        /// 武器カメラの Culling Mask と一致させるため、Weapon レイヤーに統一する。
        /// </summary>
        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 武器を SlotIndex に基づいてスロット配列に登録し、モデルを生成して _modelRegistry に追加する。
        /// Awake() からの初期登録と AddWeapon() からのピックアップ追加で共用する。
        /// </summary>
        private void RegisterWeapon(WeaponData weaponData)
        {
            int slot = weaponData.SlotIndex;
            if (slot < 0 || slot >= SlotCount) return;
            if (_weaponSlots[slot] != null) return;

            _weaponSlots[slot] = weaponData;

            if (weaponData.ModelPrefab != null)
            {
                var modelInstance = Instantiate(weaponData.ModelPrefab, transform);
                SetLayerRecursive(modelInstance, LayerMask.NameToLayer("Weapon"));
                var modelView = modelInstance.GetComponent<WeaponModelView>();
                modelInstance.SetActive(false);
                _modelRegistry[weaponData] = modelView;
            }
        }

        /// <summary>
        /// ピックアップ経由でランタイムに武器を追加する。
        /// 既に所持済みの場合は false を返す。
        /// 追加成功時は OnWeaponAdded イベントを発火し、自動的に新武器に切り替える。
        /// </summary>
        public bool AddWeapon(WeaponData weaponData)
        {
            if (weaponData == null) return false;
            int slot = weaponData.SlotIndex;
            if (slot < 0 || slot >= SlotCount) return false;
            if (_weaponSlots[slot] != null) return false;

            RegisterWeapon(weaponData);
            OnWeaponAdded?.Invoke(weaponData);

            EquipWeapon(slot);
            return true;
        }

        // --- 武器切替 ---

        private void EquipWeapon(int slotIndex)
        {
            if (_isSwitching) return;
            if (slotIndex < 0 || slotIndex >= SlotCount) return;
            if (_weaponSlots[slotIndex] == null) return;
            if (_currentWeaponData == _weaponSlots[slotIndex]) return;

            SwitchWeapon(slotIndex).Forget();
        }

        /// <summary>
        /// 武器切替の非同期シーケンス。
        /// ADS 解除 → リロードキャンセル → エフェクトプール入替 →
        /// スライドアウト/イン演出 → イベント通知 の順で実行する。
        /// _isSwitching フラグで連打による重複実行を防ぐ。
        /// </summary>
        private async UniTaskVoid SwitchWeapon(int index)
        {
            _isSwitching = true;

            if (_isAiming)
            {
                _isAiming = false;
                OnAimStateChanged?.Invoke(false);
            }

            CancelReload();

            var previousModelView = _activeModelView;

            _currentWeaponIndex = index;
            _currentWeaponData = _weaponSlots[index];
            _ammoManager.LoadAmmo(_currentWeaponData);
            _lastFireTime = 0f;

            _effectPoolManager.InitializeForWeapon(_currentWeaponData);

            // WeaponType に応じて射撃方式を自動選択（ストラテジーパターン）。
            // 新しい WeaponType を追加した場合のみ、ここに分岐を追加する。
            _fireStrategy = _currentWeaponData.WeaponType switch
            {
                WeaponType.Hitscan => new HitscanStrategy(),
                WeaponType.Projectile => new ProjectileStrategy(
                    _effectPoolManager.ImpactEffectPool, _effectPoolManager.ExplosionEffectPool,
                    _playerCamera.GetComponentInParent<Collider>()),
                _ => new HitscanStrategy()
            };

            _modelRegistry.TryGetValue(_currentWeaponData, out var incomingView);

            await PlaySwitchAnimation(previousModelView, incomingView);
            PlaySwitchSound();

            _activeModelView = incomingView;
            _activeModelView?.SetLoadedAmmoVisible(_ammoManager.CurrentAmmo > 0);
            _isSwitching = false;

            OnWeaponSwitched?.Invoke(_currentWeaponData);
            OnAmmoChanged?.Invoke(_ammoManager.CurrentAmmo, _currentWeaponData.MaxAmmo);
        }

        /// <summary>
        /// 武器切替のスライド演出。
        /// 旧武器を下方向にスライドアウトした後、新武器を下から上にスライドインさせる。
        /// DOTween の Ease で加減速をつけ、自然な動きにする。
        /// </summary>
        private async UniTask PlaySwitchAnimation(WeaponModelView outgoing, WeaponModelView incoming)
        {
            // 旧武器: 下にスライドアウト
            if (outgoing != null)
            {
                var outRest = outgoing.RestLocalPosition;
                await outgoing.transform
                    .DOLocalMoveY(outRest.y + SwitchSlideOffset, SwitchOutDuration)
                    .SetEase(Ease.InQuad)
                    .SetLink(outgoing.gameObject)
                    .AsyncWaitForCompletion();

                outgoing.transform.localPosition = outRest;
                outgoing.gameObject.SetActive(false);
            }

            // 新武器: 下から登場
            if (incoming != null)
            {
                var inRest = incoming.RestLocalPosition;
                incoming.transform.localPosition = new Vector3(inRest.x, inRest.y + SwitchSlideOffset, inRest.z);
                incoming.gameObject.SetActive(true);

                await incoming.transform
                    .DOLocalMove(inRest, SwitchInDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(incoming.gameObject)
                    .AsyncWaitForCompletion();
            }
        }

        // --- 射撃 ---

        private void TryAutoFire()
        {
            if (!_isFireHeld) return;

            // Mouse.current で実際のボタン状態を確認。
            // Game View フォーカス時のクリックでリリースイベントが届かず
            // _isFireHeld が true のまま残るケースを防ぐ。
            var mouse = Mouse.current;
            if (mouse != null && !mouse.leftButton.isPressed)
            {
                _isFireHeld = false;
                return;
            }

            TryFire();
        }

        private void TryFire()
        {
            if (_isSwitching || _isReloading || _currentWeaponData == null) return;

            if (_ammoManager.CurrentAmmo <= 0)
            {
                if (_ammoManager.ReserveAmmo > 0) StartReload().Forget();
                return;
            }

            float fireInterval = 1f / (_currentWeaponData.FireRate * _playerModel.FireRateMultiplier);
            if (Time.time - _lastFireTime < fireInterval) return;

            ExecuteFire().Forget();
        }

        private async UniTaskVoid ExecuteFire()
        {
            _lastFireTime = Time.time;
            _ammoManager.ConsumeAmmo();

            // ADS 中は拡散角を縮小し、精密射撃を可能にする
            float spreadAngle = _isAiming
                ? _currentWeaponData.AdsSpreadAngle
                : _currentWeaponData.SpreadAngle;

            // 弾道はカメラ中心から発射し、クロスヘアが指す方向に飛ぶ。
            // マズルフラッシュは銃口(WeaponModelView.MuzzlePoint)に表示する。
            var result = await _fireStrategy.Fire(
                _currentWeaponData, _playerCamera.transform, _hitMask,
                spreadAngle, _playerModel.DamageMultiplier);

            _activeModelView?.PlayMuzzleFlash();
            PlayFireSound();
            _activeModelView?.SetLoadedAmmoVisible(false);

            if (result.DidHit)
            {
                _effectPoolManager.SpawnImpactEffect(result.HitPoint, result.HitNormal);
            }

            _activeModelView?.PlayKick(_currentWeaponData);

            OnAmmoChanged?.Invoke(_ammoManager.CurrentAmmo, _currentWeaponData.MaxAmmo);
            OnFired?.Invoke();
            EventBus.Publish(new WeaponFiredEvent());
        }

        // --- サウンド ---

        private void PlaySwitchSound()
        {
            AudioHelper.PlaySE(_currentWeaponData?.SwitchSound);
        }

        private void PlayFireSound()
        {
            AudioHelper.PlaySE(_currentWeaponData?.FireSound);
        }

        private void PlayReloadStartSound()
        {
            AudioHelper.PlaySE(_currentWeaponData?.ReloadStartSound);
        }

        private void PlayReloadEndSound()
        {
            AudioHelper.PlaySE(_currentWeaponData?.ReloadEndSound);
        }

        // --- リロード ---

        /// <summary>
        /// リロードを開始する。UniTask で ReloadTime 分待機し、弾薬を全回復する。
        /// 武器切替やオブジェクト破棄時は CancellationToken でキャンセルされる。
        /// </summary>
        private async UniTask StartReload()
        {
            if (_isSwitching || _isReloading || _currentWeaponData == null) return;
            if (_ammoManager.IsFull(_currentWeaponData.MaxAmmo)) return;
            if (_ammoManager.ReserveAmmo <= 0) return;

            CancelReload();
            _reloadCts = new CancellationTokenSource();

            _isReloading = true;
            OnReloadStateChanged?.Invoke(true);
            PlayReloadStartSound();

            try
            {
                float adjustedReloadTime = _currentWeaponData.ReloadTime / _playerModel.ReloadSpeedMultiplier;
                await UniTask.Delay(
                    TimeSpan.FromSeconds(adjustedReloadTime),
                    cancellationToken: _reloadCts.Token);

                PlayReloadEndSound();
                _activeModelView?.SetLoadedAmmoVisible(true);

                _ammoManager.Reload(_currentWeaponData.MaxAmmo);
                OnAmmoChanged?.Invoke(_ammoManager.CurrentAmmo, _currentWeaponData.MaxAmmo);
            }
            catch (OperationCanceledException)
            {
                // 武器切替やオブジェクト破棄によるキャンセルは正常動作
            }
            finally
            {
                _isReloading = false;
                // OnDestroy 経由のキャンセル時は購読側が既に破棄されている可能性があるため
                // イベントを発火しない
                if (!_isDestroying)
                {
                    OnReloadStateChanged?.Invoke(false);
                }
            }
        }

        private void CancelReload()
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;
        }

        /// <summary>
        /// 指定した弾薬タイプのリザーブを補充する。AmmoPickup や WeaponPickup から呼ばれる公開メソッド。
        /// </summary>
        public void AddReserveAmmo(AmmoType type, int amount)
        {
            _ammoManager.AddReserve(type, amount);
            OnAmmoChanged?.Invoke(_ammoManager.CurrentAmmo, _currentWeaponData?.MaxAmmo ?? 0);
        }

        // --- ADS FOV ---

        private void SetWeaponCameraEnabled(bool enabled)
        {
            if (_weaponCamera != null && _weaponCamera.enabled != enabled)
            {
                _weaponCamera.enabled = enabled;
            }
        }

        /// <summary>
        /// ADS 中はカメラ FOV を武器ごとの設定値に滑らかに遷移させる。
        /// Lerp による補間で急激な視界変化を防ぐ。
        /// 武器カメラの FOV もメインカメラと同期させ、
        /// ADS 時にズーム感が武器モデルにも反映されるようにする。
        /// </summary>
        private void UpdateAdsFieldOfView()
        {
            if (_playerCamera == null) return;

            float targetFov = _isAiming
                ? _currentWeaponData.AdsFieldOfView
                : _defaultFieldOfView;

            float newFov = Mathf.Lerp(
                _playerCamera.fieldOfView,
                targetFov,
                _adsFovLerpSpeed * Time.deltaTime);

            _playerCamera.fieldOfView = newFov;

            if (_weaponCamera != null)
            {
                _weaponCamera.fieldOfView = newFov;
            }
        }
    }
}
