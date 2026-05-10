using UnityEngine;
using Velora.Core;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 地面に配置する弾薬ピックアップオブジェクト。
    /// トリガー接触でプレイヤーの WeaponController にリザーブ弾薬を補充し、自身を破棄する。
    /// _ammoEntries で複数の弾薬タイプ・補充量を自由に組み合わせられる。
    /// 例: Light 30 + Energy 5 を1つのピックアップで配布することも可能。
    /// 回転・浮遊演出は PickupBobAnimation コンポーネントが担当する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PickupBobAnimation))]
    public class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private AmmoEntry[] _ammoEntries;

        private void OnTriggerEnter(Collider other)
        {
            var weaponController = other.GetComponentInChildren<WeaponController>();
            if (weaponController == null) return;

            foreach (var entry in _ammoEntries)
            {
                weaponController.AddReserveAmmo(entry.Type, entry.Amount);
            }

            EventBus.Publish(new AmmoPickedUpEvent());
            Destroy(gameObject);
        }
    }
}
