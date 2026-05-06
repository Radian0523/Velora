using System.Collections.Generic;
using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 武器ごとの弾薬状態（装填数・リザーブ）を管理する純粋 C# クラス。
    /// WeaponController から弾薬の CRUD 操作を分離し、単一責任原則を実現する。
    /// 武器切替時は LoadAmmo で弾数を復元し、射撃・リロード後は自動的に保存する。
    /// </summary>
    public class WeaponAmmoManager
    {
        private int _currentAmmo;
        private int _reserveAmmo;
        private WeaponData _activeWeapon;
        private readonly Dictionary<WeaponData, int> _ammoMap = new();

        public int CurrentAmmo => _currentAmmo;
        public int ReserveAmmo => _reserveAmmo;

        public WeaponAmmoManager(int initialReserveAmmo)
        {
            _reserveAmmo = initialReserveAmmo;
        }

        /// <summary>
        /// 武器に対応する弾数を復元する。初回装備時は MaxAmmo で初期化する。
        /// 切替先の武器を _activeWeapon として記録し、以降の保存で使用する。
        /// </summary>
        public void LoadAmmo(WeaponData weaponData)
        {
            _activeWeapon = weaponData;
            _currentAmmo = _ammoMap.TryGetValue(weaponData, out int saved)
                ? saved
                : weaponData.MaxAmmo;
        }

        /// <summary>
        /// 1発消費し、弾数を保存する。
        /// </summary>
        public void ConsumeAmmo()
        {
            _currentAmmo--;
            Save();
        }

        /// <summary>
        /// リザーブから必要分だけ補充する（リソース消費型リロード）。
        /// </summary>
        public void Reload(int maxAmmo)
        {
            int needed = maxAmmo - _currentAmmo;
            int toLoad = Mathf.Min(needed, _reserveAmmo);
            _currentAmmo += toLoad;
            _reserveAmmo -= toLoad;
            Save();
        }

        public bool IsFull(int maxAmmo) => _currentAmmo >= maxAmmo;

        /// <summary>
        /// リザーブ弾薬を補充する。AmmoPickup 等から呼ばれる。
        /// </summary>
        public void AddReserve(int amount)
        {
            _reserveAmmo += amount;
        }

        private void Save()
        {
            if (_activeWeapon == null) return;
            _ammoMap[_activeWeapon] = _currentAmmo;
        }
    }
}
