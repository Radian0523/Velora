using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Velora.Core;
using Velora.Data;
using Velora.Player;

namespace Velora.Weapon
{
    /// <summary>
    /// 射撃・リロード・ADS・武器切替・リコイル・視覚フィードバックを統合管理する MonoBehaviour。
    /// 各武器の射撃方式は IFireStrategy で差し替え可能（ストラテジーパターン）。
    /// エフェクト生成は Strategy から分離し、WeaponController が一元管理する。
    /// これにより射撃ロジック（Strategy）と視覚演出（Controller）の責務が分離される。
    ///
    /// 武器モデルは WeaponData.ModelPrefab から生成し、_modelRegistry で管理する。
    /// 初期武器は _initialWeapons（Inspector 設定）から Start 時に登録。
    /// ランタイムでは AddWeapon() でピックアップ経由の武器追加に対応する。
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("武器データ（初期装備）")]
        [FormerlySerializedAs("_weapons")]
        [SerializeField] private WeaponData[] _initialWeapons;

        // ランタイムで管理する所持武器リスト。
        // 初期装備 + ピックアップで追加された武器を保持する。
        private readonly List<WeaponData> _ownedWeapons = new();

        [Header("参照")]
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Camera _weaponCamera;
        [SerializeField] private FPSController _fpsController;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private ParticleSystem _muzzleFlashVfx;

        [Header("ADS")]
        [SerializeField] private float _defaultFieldOfView = 60f;
        [SerializeField] private float _adsFovLerpSpeed = 10f;

        private WeaponData _currentWeaponData;
        private IFireStrategy _fireStrategy;
        private int _currentWeaponIndex;
        private int _currentAmmo;
        private float _lastFireTime;
        private bool _isReloading;
        private bool _isAiming;
        private bool _isFireHeld;
        private bool _isSwitching;

        // 武器モデル管理: WeaponData → 生成済み WeaponModelView のマッピング。
        // Start 時に全武器分を生成し、切替時は SetActive で表示を切り替える。
        private readonly Dictionary<WeaponData, WeaponModelView> _modelRegistry = new();
        private WeaponModelView _activeModelView;

        // リコイル: カメラに適用した実際の pitch/yaw オフセットを追跡し、
        // 射撃停止後に逆方向へ復帰させる
        private int _consecutiveShotCount;
        private Vector2 _recoilCameraOffset;

        // 着弾エフェクトプール: 武器ごとにプールを生成し、武器切替時に入れ替える
        private ObjectPool<PooledEffect> _impactEffectPool;

        private const int EffectPoolInitialSize = 3;
        private const int EffectPoolMaxSize = 10;

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

        public WeaponData CurrentWeaponData => _currentWeaponData;
        public IReadOnlyList<WeaponData> Weapons => _ownedWeapons;
        public int CurrentWeaponIndex => _currentWeaponIndex;
        public bool IsAiming => _isAiming;
        public int CurrentAmmo => _currentAmmo;

        private void Start()
        {
            if (_initialWeapons == null || _initialWeapons.Length == 0) return;

            // マズルフラッシュは武器カメラで描画するため Weapon レイヤーに設定。
            // メインカメラの Culling Mask から除外されていても正しく表示される。
            if (_muzzleFlashVfx != null)
            {
                SetLayerRecursive(_muzzleFlashVfx.gameObject, LayerMask.NameToLayer("Weapon"));
            }

            // Inspector で設定された初期武器をランタイムリストに登録し、
            // モデルを事前生成して非アクティブで待機させる。
            foreach (var weaponData in _initialWeapons)
            {
                if (weaponData == null) continue;
                RegisterWeapon(weaponData);
            }

            if (_ownedWeapons.Count > 0)
            {
                EquipWeapon(0);
            }
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
            UpdateRecoilRecovery();
            UpdateAdsFieldOfView();
        }

        private void OnDestroy()
        {
            CancelReload();
            CleanupEffectPools();

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
            else
            {
                _consecutiveShotCount = 0;
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
            if (scroll == 0f || _ownedWeapons.Count <= 1) return;

            int direction = scroll > 0f ? 1 : -1;
            int nextIndex = (_currentWeaponIndex + direction + _ownedWeapons.Count) % _ownedWeapons.Count;
            EquipWeapon(nextIndex);
        }

        public void OnWeapon1(InputValue value) { if (value.isPressed) EquipWeapon(0); }
        public void OnWeapon2(InputValue value) { if (value.isPressed) EquipWeapon(1); }
        public void OnWeapon3(InputValue value) { if (value.isPressed) EquipWeapon(2); }
        public void OnWeapon4(InputValue value) { if (value.isPressed) EquipWeapon(3); }
        public void OnWeapon5(InputValue value) { if (value.isPressed) EquipWeapon(4); }
        public void OnWeapon6(InputValue value) { if (value.isPressed) EquipWeapon(5); }

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
        /// 武器をランタイムリストに登録し、モデルを生成して _modelRegistry に追加する。
        /// Start() からの初期登録と AddWeapon() からのピックアップ追加で共用する。
        /// </summary>
        private void RegisterWeapon(WeaponData weaponData)
        {
            if (_ownedWeapons.Contains(weaponData)) return;

            _ownedWeapons.Add(weaponData);

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
            if (_ownedWeapons.Contains(weaponData)) return false;

            RegisterWeapon(weaponData);
            OnWeaponAdded?.Invoke(weaponData);

            // 追加した武器に即座に切り替える
            EquipWeapon(_ownedWeapons.Count - 1);
            return true;
        }

        // --- 武器切替 ---

        private void EquipWeapon(int index)
        {
            if (_isSwitching) return;
            if (index < 0 || index >= _ownedWeapons.Count) return;
            if (_ownedWeapons[index] == null) return;
            if (_currentWeaponData == _ownedWeapons[index]) return;

            SwitchWeapon(index).Forget();
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
            CleanupEffectPools();

            var previousModelView = _activeModelView;

            _currentWeaponIndex = index;
            _currentWeaponData = _ownedWeapons[index];
            _currentAmmo = _currentWeaponData.MaxAmmo;
            _lastFireTime = 0f;
            _consecutiveShotCount = 0;
            _recoilCameraOffset = Vector2.zero;

            InitializeEffectPools();

            // WeaponType に応じて射撃方式を自動選択（ストラテジーパターン）。
            // 新しい WeaponType を追加した場合のみ、ここに分岐を追加する。
            _fireStrategy = _currentWeaponData.WeaponType switch
            {
                WeaponType.Hitscan => new HitscanStrategy(),
                WeaponType.Projectile => new ProjectileStrategy(_impactEffectPool),
                _ => new HitscanStrategy()
            };

            _modelRegistry.TryGetValue(_currentWeaponData, out var incomingView);

            await PlaySwitchAnimation(previousModelView, incomingView);

            _activeModelView = incomingView;
            _isSwitching = false;

            OnWeaponSwitched?.Invoke(_currentWeaponData);
            OnAmmoChanged?.Invoke(_currentAmmo, _currentWeaponData.MaxAmmo);
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

        // --- エフェクトプール ---

        /// <summary>
        /// 武器装備時にエフェクトプールを生成する。
        /// プレハブに PooledEffect がアタッチされていない場合はプールを作らない（エフェクトなし武器に対応）。
        /// </summary>
        private void InitializeEffectPools()
        {
            _impactEffectPool = CreateEffectPool(
                _currentWeaponData.ImpactEffectPrefab,
                $"Pool_{_currentWeaponData.WeaponName}_ImpactEffect");
        }

        private ObjectPool<PooledEffect> CreateEffectPool(GameObject prefab, string poolName)
        {
            if (prefab == null) return null;

            var pooledEffect = prefab.GetComponent<PooledEffect>();
            if (pooledEffect == null) return null;

            var poolParent = new GameObject(poolName).transform;
            var pool = new ObjectPool<PooledEffect>(pooledEffect, poolParent, EffectPoolInitialSize, EffectPoolMaxSize);

            // プール内の全インスタンスに自身のプール参照を設定
            // Get() 時に取得されるインスタンスにも Initialize が必要なため、
            // ObjectPool の Get → Initialize → Return のサイクルで設定する
            for (int i = 0; i < EffectPoolInitialSize; i++)
            {
                var instance = pool.Get();
                instance.Initialize(pool);
                pool.Return(instance);
            }

            return pool;
        }

        private void CleanupEffectPools()
        {
            _impactEffectPool?.Clear();
            _impactEffectPool = null;
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
                _consecutiveShotCount = 0;
                return;
            }

            TryFire();
        }

        private void TryFire()
        {
            if (_isSwitching || _isReloading || _currentWeaponData == null) return;

            if (_currentAmmo <= 0)
            {
                StartReload().Forget();
                return;
            }

            float fireInterval = 1f / _currentWeaponData.FireRate;
            if (Time.time - _lastFireTime < fireInterval) return;

            ExecuteFire().Forget();
        }

        private async UniTaskVoid ExecuteFire()
        {
            _lastFireTime = Time.time;
            _currentAmmo--;

            // ADS 中は拡散角を縮小し、精密射撃を可能にする
            float spreadAngle = _isAiming
                ? _currentWeaponData.AdsSpreadAngle
                : _currentWeaponData.SpreadAngle;

            // 弾道はカメラ中心から発射し、クロスヘアが指す方向に飛ぶ。
            // マズルフラッシュは銃口(WeaponModelView.MuzzlePoint)に表示する。
            var result = await _fireStrategy.Fire(_currentWeaponData, _playerCamera.transform, _hitMask, spreadAngle);

            SpawnMuzzleFlash();

            if (result.DidHit)
            {
                SpawnImpactEffect(result);
            }

            ApplyWeaponKick();

            OnAmmoChanged?.Invoke(_currentAmmo, _currentWeaponData.MaxAmmo);
            OnFired?.Invoke();
            EventBus.Publish(new WeaponFiredEvent());
        }

        // --- 視覚フィードバック ---

        /// <summary>
        /// マズルフラッシュを現在の武器モデルの銃口位置にスナップしてから再生する。
        /// 共有 VFX を使い回すことでプール不要にしつつ、武器ごとに正しい位置で表示する。
        /// </summary>
        private void SpawnMuzzleFlash()
        {
            if (_muzzleFlashVfx == null) return;

            if (_activeModelView != null && _activeModelView.MuzzlePoint != null)
            {
                _muzzleFlashVfx.transform.position = _activeModelView.MuzzlePoint.position;
            }

            _muzzleFlashVfx.Play();
        }

        private void SpawnImpactEffect(FireResult result)
        {
            if (_impactEffectPool == null) return;

            var effect = _impactEffectPool.Get();
            effect.Initialize(_impactEffectPool);
            effect.transform.SetPositionAndRotation(result.HitPoint, Quaternion.LookRotation(result.HitNormal));
        }

        /// <summary>
        /// 発射時の武器モデルキック演出。
        /// DOTween の Punch で後退 + 上方向回転を同時適用し、自然な反動を再現する。
        /// パラメータは WeaponData で武器ごとに調整可能（データドリブン）。
        /// </summary>
        private void ApplyWeaponKick()
        {
            if (_activeModelView == null || _currentWeaponData == null) return;

            var modelTransform = _activeModelView.transform;
            modelTransform.DOComplete();

            modelTransform.DOPunchPosition(
                Vector3.back * _currentWeaponData.KickBackDistance,
                _currentWeaponData.KickDuration,
                _currentWeaponData.KickVibrato);

            modelTransform.DOPunchRotation(
                Vector3.right * -_currentWeaponData.KickUpAngle,
                _currentWeaponData.KickDuration,
                _currentWeaponData.KickVibrato);
        }

        // --- リコイル ---

        private void ApplyRecoil()
        {
            if (_currentWeaponData.RecoilPattern == null || _fpsController == null) return;

            var recoil = _currentWeaponData.RecoilPattern;

            // 連射の進行度に応じて反動の強さが変化する（AnimationCurve で定義）
            float t = Mathf.Clamp01((float)_consecutiveShotCount / _currentWeaponData.MaxAmmo);
            float vertical = recoil.VerticalRecoil.Evaluate(t);
            float horizontal = recoil.HorizontalRecoil.Evaluate(t);

            // pitch は負方向が上向きなので、反動で上を向かせるには負の値を渡す
            float pitchDelta = -vertical;
            float yawDelta = horizontal;

            _fpsController.AddCameraRecoil(pitchDelta, yawDelta);
            _recoilCameraOffset += new Vector2(pitchDelta, yawDelta);
            _consecutiveShotCount++;
        }

        /// <summary>
        /// 射撃停止後にリコイルの蓄積分を徐々にゼロへ戻す。
        /// カメラに適用した pitch/yaw オフセットを逆方向に補間して復帰させる。
        /// </summary>
        private void UpdateRecoilRecovery()
        {
            if (_recoilCameraOffset.sqrMagnitude < 0.001f)
            {
                _recoilCameraOffset = Vector2.zero;
                return;
            }

            if (_isFireHeld) return;
            if (_currentWeaponData?.RecoilPattern == null || _fpsController == null) return;

            float recoverySpeed = _currentWeaponData.RecoilPattern.RecoverySpeed * Time.deltaTime;
            var newOffset = Vector2.MoveTowards(_recoilCameraOffset, Vector2.zero, recoverySpeed);
            var recoveryDelta = newOffset - _recoilCameraOffset;

            _fpsController.AddCameraRecoil(recoveryDelta.x, recoveryDelta.y);
            _recoilCameraOffset = newOffset;
        }

        // --- リロード ---

        /// <summary>
        /// リロードを開始する。UniTask で ReloadTime 分待機し、弾薬を全回復する。
        /// 武器切替やオブジェクト破棄時は CancellationToken でキャンセルされる。
        /// </summary>
        private async UniTask StartReload()
        {
            if (_isSwitching || _isReloading || _currentWeaponData == null) return;
            if (_currentAmmo >= _currentWeaponData.MaxAmmo) return;

            CancelReload();
            _reloadCts = new CancellationTokenSource();

            _isReloading = true;
            OnReloadStateChanged?.Invoke(true);

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_currentWeaponData.ReloadTime),
                    cancellationToken: _reloadCts.Token);

                _currentAmmo = _currentWeaponData.MaxAmmo;
                OnAmmoChanged?.Invoke(_currentAmmo, _currentWeaponData.MaxAmmo);
            }
            catch (OperationCanceledException)
            {
                // 武器切替やオブジェクト破棄によるキャンセルは正常動作
            }
            finally
            {
                _isReloading = false;
                OnReloadStateChanged?.Invoke(false);
            }
        }

        private void CancelReload()
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;
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
