using UnityEngine;
using Velora.Core;

namespace Velora.Weapon
{
    /// <summary>
    /// 地面に配置する弾薬ピックアップオブジェクト。
    /// トリガー接触でプレイヤーの WeaponController にリザーブ弾薬を補充し、自身を破棄する。
    /// 回転・浮遊演出は PickupBobAnimation コンポーネントが担当する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PickupBobAnimation))]
    public class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private int _ammoAmount = 30;

        private void OnTriggerEnter(Collider other)
        {
            var weaponController = other.GetComponentInChildren<WeaponController>();
            if (weaponController == null) return;

            weaponController.AddReserveAmmo(_ammoAmount);
            EventBus.Publish(new AmmoPickedUpEvent());
            Destroy(gameObject);
        }
    }
}
