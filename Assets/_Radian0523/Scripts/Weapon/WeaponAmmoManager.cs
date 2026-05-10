using System.Collections.Generic;
using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 武器ごとの弾薬状態（装填数・リザーブ）を管理する純粋 C# クラス。
    /// WeaponController から弾薬の CRUD 操作を分離し、単一責任原則を実現する。
    /// 武器切替時は LoadAmmo で弾数を復元し、射撃・リロード後は自動的に保存する。
    ///
    /// リザーブは AmmoType ごとに独立したプールを持つ。
    /// 武器間で弾薬消費が干渉しないことで、各武器の運用コストを明確にし、
    /// DOOM Eternal スタイルの弾薬管理を実現する。
    /// </summary>
    public class WeaponAmmoManager
    {
        private int _currentAmmo;
        private WeaponData _activeWeapon;
        private readonly Dictionary<WeaponData, int> _ammoMap = new();

        // 弾薬タイプごとの独立したリザーブ。
        // 武器間で弾薬消費が干渉しないことで、各武器の運用コストを明確にする。
        private readonly Dictionary<AmmoType, int> _reserveMap = new();

        public int CurrentAmmo => _currentAmmo;

        /// <summary>
        /// 現在装備中の武器の弾薬タイプに対応するリザーブを返す。
        /// HudPresenter は従来通り ReserveAmmo を参照するだけで正しい値が得られる。
        /// </summary>
        public int ReserveAmmo =>
            _activeWeapon != null && _reserveMap.TryGetValue(_activeWeapon.AmmoType, out int reserve)
                ? reserve : 0;

        public WeaponAmmoManager(Dictionary<AmmoType, int> initialReserves)
        {
            foreach (var kvp in initialReserves)
            {
                _reserveMap[kvp.Key] = kvp.Value;
            }
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
        /// 現在装備中の武器の弾薬タイプに対応するリザーブから必要分だけ補充する。
        /// </summary>
        public void Reload(int maxAmmo)
        {
            if (_activeWeapon == null) return;

            var type = _activeWeapon.AmmoType;
            int reserve = _reserveMap.GetValueOrDefault(type, 0);
            int needed = maxAmmo - _currentAmmo;
            int toLoad = Mathf.Min(needed, reserve);
            _currentAmmo += toLoad;
            _reserveMap[type] = reserve - toLoad;
            Save();
        }

        public bool IsFull(int maxAmmo) => _currentAmmo >= maxAmmo;

        /// <summary>
        /// 指定した弾薬タイプのリザーブを補充する。AmmoPickup / WeaponPickup から呼ばれる。
        /// </summary>
        public void AddReserve(AmmoType type, int amount)
        {
            _reserveMap[type] = _reserveMap.GetValueOrDefault(type, 0) + amount;
        }

        private void Save()
        {
            if (_activeWeapon == null) return;
            _ammoMap[_activeWeapon] = _currentAmmo;
        }
    }
}
