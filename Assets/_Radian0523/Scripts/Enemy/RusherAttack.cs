using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 近接攻撃ビヘイビア。
    /// ウィンドアップ後に距離チェックを行い、射程内ならプレイヤーにダメージを適用する。
    /// </summary>
    public class RusherAttack : IAttackBehavior
    {
        private const float WindupDuration = 0.3f;

        public async UniTask Attack(EnemyController controller)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(WindupDuration),
                cancellationToken: controller.destroyCancellationToken);

            if (controller.Model.IsDead) return;

            float distance = Vector3.Distance(
                controller.transform.position,
                controller.PlayerTransform.position);

            if (distance <= controller.Data.AttackRange)
            {
                controller.PlayerDamageable.TakeDamage(
                    controller.Data.AttackDamage,
                    controller.transform.position,
                    false);
            }
        }
    }
}
