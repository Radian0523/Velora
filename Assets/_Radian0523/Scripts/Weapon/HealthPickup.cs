using UnityEngine;
using Velora.Core;
using Velora.Player;

namespace Velora.Weapon
{
    /// <summary>
    /// 回復ピックアップオブジェクト。AmmoPickup と同パターンで、
    /// トリガー接触でプレイヤーの HP を回復し、自身を破棄する。
    /// 回復量は Inspector で設定し、DropTableData 経由で出現を制御する。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(PickupBobAnimation))]
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 25f;

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.GetComponent<PlayerDamageReceiver>();
            if (receiver == null) return;

            receiver.Heal(_healAmount);
            EventBus.Publish(new HealthPickedUpEvent());
            Destroy(gameObject);
        }
    }
}
