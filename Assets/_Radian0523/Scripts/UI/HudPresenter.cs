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
                    false);
            }

            _playerModel.OnHealthChanged += HandleHealthChanged;
            _weaponController.OnAmmoChanged += HandleAmmoChanged;
            _weaponController.OnReloadStateChanged += HandleReloadStateChanged;

            // 武器切替時にも弾数を更新する。
            // WeaponController.Start() が先に走って EquipWeapon(0) が発火した場合でも
            // OnWeaponSwitched を購読しておくことで初期弾数を取り逃さない。
            _weaponController.OnWeaponSwitched += HandleWeaponSwitched;

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
            }

            EventBus.Unsubscribe<WaveStartedEvent>(HandleWaveStarted);
        }

        private void HandleHealthChanged(float current, float max)
        {
            _hudView.UpdateHealthBar(current, max);
        }

        private void HandleAmmoChanged(int current, int max)
        {
            _hudView.UpdateAmmoDisplay(current, max, _isReloading);
        }

        private void HandleReloadStateChanged(bool isReloading)
        {
            _isReloading = isReloading;

            if (_weaponController.CurrentWeaponData != null)
            {
                _hudView.UpdateAmmoDisplay(
                    _weaponController.CurrentAmmo,
                    _weaponController.CurrentWeaponData.MaxAmmo,
                    isReloading);
            }
        }

        private void HandleWeaponSwitched(WeaponData weaponData)
        {
            _isReloading = false;
            _hudView.UpdateAmmoDisplay(_weaponController.CurrentAmmo, weaponData.MaxAmmo, false);
        }

        private void HandleWaveStarted(WaveStartedEvent e)
        {
            _hudView.ShowWaveNumber(e.WaveNumber);
        }
    }
}
