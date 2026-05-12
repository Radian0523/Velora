using UnityEngine;
using Velora.Core;
using Velora.Data;
using Velora.Player;
using Velora.Weapon;

namespace Velora.UI
{
    /// <summary>
    /// PlayerModel・WeaponController・WaveDirector のイベントを購読し、
    /// HudView へ変換して渡す Presenter。
    /// View と Model を直接参照させないことで、
    /// HUD の表示形式変更時に Model 側の変更が不要になる。
    /// </summary>
    public class HudPresenter : MonoBehaviour
    {
        [SerializeField] private HudView _hudView;

        [Header("弾薬タイプ別テキスト色")]
        [SerializeField] private Color _lightAmmoColor = new Color(1f, 0.843f, 0f);
        [SerializeField] private Color _energyAmmoColor = new Color(0f, 0.898f, 1f);
        [SerializeField] private Color _explosiveAmmoColor = new Color(1f, 0.188f, 0.188f);

        private PlayerModel _playerModel;
        private WeaponController _weaponController;
        private bool _isReloading;

        public void Initialize(PlayerModel playerModel, WeaponController weaponController)
        {
            _playerModel = playerModel;
            _weaponController = weaponController;

            // 初期値を反映する
            _hudView.UpdateHealthBar(_playerModel.CurrentHealth, _playerModel.MaxHealth);

            // WeaponController.Start() より後に呼ばれる場合は初期弾数が設定済みのため即反映
            if (_weaponController.CurrentWeaponData != null)
            {
                _hudView.UpdateAmmoDisplay(
                    _weaponController.CurrentAmmo,
                    _weaponController.CurrentWeaponData.MaxAmmo,
                    _weaponController.ReserveAmmo,
                    false,
                    GetAmmoTypeColor(_weaponController.CurrentWeaponData.AmmoType));
            }

            _playerModel.OnHealthChanged += HandleHealthChanged;
            _weaponController.OnAmmoChanged += HandleAmmoChanged;
            _weaponController.OnReloadStateChanged += HandleReloadStateChanged;

            // 武器切替時にも弾数を更新する。
            // WeaponController.Start() が先に走って EquipWeapon(0) が発火した場合でも
            // OnWeaponSwitched を購読しておくことで初期弾数を取り逃さない。
            _weaponController.OnWeaponSwitched += HandleWeaponSwitched;
            _weaponController.OnWeaponAdded += HandleWeaponAdded;

            // 空スロットを生成した後、所持済み武器を SlotIndex に基づいて割り当てる
            _hudView.InitializeWeaponBar();
            for (int i = 0; i < _weaponController.Weapons.Count; i++)
            {
                var weapon = _weaponController.Weapons[i];
                if (weapon != null)
                {
                    _hudView.AssignWeaponToSlot(i, weapon.Icon);
                }
            }
            _hudView.SelectWeaponSlot(_weaponController.CurrentWeaponIndex);

            EventBus.Subscribe<WaveStartedEvent>(HandleWaveStarted);
        }

        private void OnDestroy()
        {
            if (_playerModel != null)
            {
                _playerModel.OnHealthChanged -= HandleHealthChanged;
            }

            if (_weaponController != null)
            {
                _weaponController.OnAmmoChanged -= HandleAmmoChanged;
                _weaponController.OnReloadStateChanged -= HandleReloadStateChanged;
                _weaponController.OnWeaponSwitched -= HandleWeaponSwitched;
                _weaponController.OnWeaponAdded -= HandleWeaponAdded;
            }

            EventBus.Unsubscribe<WaveStartedEvent>(HandleWaveStarted);
        }

        private void HandleHealthChanged(float current, float max)
        {
            _hudView.UpdateHealthBar(current, max);
        }

        private void HandleAmmoChanged(int current, int max)
        {
            _hudView.UpdateAmmoDisplay(
                current, max, _weaponController.ReserveAmmo, _isReloading,
                GetAmmoTypeColor(_weaponController.CurrentWeaponData.AmmoType));
        }

        private void HandleReloadStateChanged(bool isReloading)
        {
            _isReloading = isReloading;

            if (_weaponController.CurrentWeaponData != null)
            {
                _hudView.UpdateAmmoDisplay(
                    _weaponController.CurrentAmmo,
                    _weaponController.CurrentWeaponData.MaxAmmo,
                    _weaponController.ReserveAmmo,
                    isReloading,
                    GetAmmoTypeColor(_weaponController.CurrentWeaponData.AmmoType));

                if (isReloading)
                {
                    float adjustedReloadTime = _weaponController.CurrentWeaponData.ReloadTime
                                             / _playerModel.ReloadSpeedMultiplier;
                    _hudView.ShowReloadRing(adjustedReloadTime);
                }
                else
                {
                    _hudView.HideReloadRing();
                }
            }
        }

        private void HandleWeaponSwitched(WeaponData weaponData)
        {
            _isReloading = false;
            _hudView.UpdateAmmoDisplay(
                _weaponController.CurrentAmmo, weaponData.MaxAmmo,
                _weaponController.ReserveAmmo, false,
                GetAmmoTypeColor(weaponData.AmmoType));
            _hudView.SelectWeaponSlot(_weaponController.CurrentWeaponIndex);
            _hudView.HideReloadRing();
        }

        private void HandleWeaponAdded(WeaponData weaponData)
        {
            _hudView.AssignWeaponToSlot(weaponData.SlotIndex, weaponData.Icon);
        }

        private void HandleWaveStarted(WaveStartedEvent e)
        {
            _hudView.ShowWaveNumber(e.WaveNumber);
        }

        private Color GetAmmoTypeColor(AmmoType ammoType)
        {
            return ammoType switch
            {
                AmmoType.Light => _lightAmmoColor,
                AmmoType.Energy => _energyAmmoColor,
                AmmoType.Explosive => _explosiveAmmoColor,
                _ => Color.white
            };
        }
    }
}
