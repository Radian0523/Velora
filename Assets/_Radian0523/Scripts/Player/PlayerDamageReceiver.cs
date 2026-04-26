using UnityEngine;
using Velora.Battle;
using Velora.Core;

namespace Velora.Player
{
    /// <summary>
    /// プレイヤーのダメージ受付を担当する MonoBehaviour。
    /// IDamageable を実装し、敵の攻撃を PlayerModel に橋渡しする。
    /// PlayerModel は Initialize で受け取る（VContainer の [Inject] にも対応可能）。
    /// </summary>
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        private PlayerModel _playerModel;

        public void Initialize(PlayerModel playerModel)
        {
            _playerModel = playerModel;
        }

        public void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot)
        {
            if (_playerModel == null || _playerModel.IsDead) return;

            _playerModel.TakeDamage(damage);
            EventBus.Publish(new PlayerDamagedEvent(damage));
        }
    }
}
