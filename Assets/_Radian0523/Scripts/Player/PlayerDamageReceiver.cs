using UnityEngine;
using Velora.Battle;
using Velora.Core;

namespace Velora.Player
{
    /// <summary>
    /// プレイヤーのダメージ受付と HP 変化通知を担当する MonoBehaviour。
    /// IDamageable を実装し、敵の攻撃を PlayerModel に橋渡しする。
    /// PlayerModel.OnHealthChanged を EventBus に中継することで、
    /// ダメージ・回復・アップグレードなど全ての HP 変動を一元的に通知する。
    /// </summary>
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        private PlayerModel _playerModel;

        public void Initialize(PlayerModel playerModel)
        {
            _playerModel = playerModel;
            _playerModel.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (_playerModel != null)
            {
                _playerModel.OnHealthChanged -= HandleHealthChanged;
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot)
        {
            if (_playerModel == null || _playerModel.IsDead) return;

            _playerModel.TakeDamage(damage);
            EventBus.Publish(new PlayerDamagedEvent(damage));
        }

        private void HandleHealthChanged(float current, float max)
        {
            EventBus.Publish(new PlayerHealthChangedEvent(current, max));
        }
    }
}
