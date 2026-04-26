using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Battle;

namespace Velora.Enemy
{
    /// <summary>
    /// 遠距離攻撃ビヘイビア。
    /// エイム後にプレイヤー方向へレイキャストし、命中時にダメージを適用する。
    /// ProjectilePrefab 未設定でもレイキャストで動作する null-safe 設計。
    /// </summary>
    public class RangedAttack : IAttackBehavior
    {
        private const float AimDuration = 0.3f;
        private const float EyeHeightOffset = 1.5f;

        public async UniTask Attack(EnemyController controller)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(AimDuration),
                cancellationToken: controller.destroyCancellationToken);

            if (controller.Model.IsDead) return;

            var origin = controller.transform.position + Vector3.up * EyeHeightOffset;
            var targetPoint = controller.PlayerTransform.position + Vector3.up * EyeHeightOffset;
            var direction = (targetPoint - origin).normalized;

            if (Physics.Raycast(origin, direction, out var hit, controller.Data.AttackRange))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(controller.Data.AttackDamage, hit.point, false);
                }
            }
        }
    }
}
