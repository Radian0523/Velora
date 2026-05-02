using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Core;
using Velora.Data;

namespace Velora.Enemy
{
    /// <summary>
    /// 遠距離攻撃ビヘイビア。
    /// 頭上にチャージエフェクトとプロジェクタイルを表示し、
    /// チャージ完了後にプレイヤー方向へ放出する。
    /// チャージ演出がプレイヤーへの視覚テレグラフとなり、回避判断の猶予を与える。
    /// 全 RangedAttack インスタンスが静的プールを共有し、
    /// シーン遷移で親が破棄されても次回 Attack 時に自動再生成される。
    /// </summary>
    public class RangedAttack : IAttackBehavior
    {
        private const float AttackAnimationDuration = 0.5f;
        private const float ChargeHeightOffset = 2.0f;
        private const float PlayerEyeHeightOffset = 1.5f;
        private const int PoolInitialSize = 5;
        private const int PoolMaxSize = 20;

        // 全 RangedAttack インスタンスで1つのプールを共有する。
        // シーン遷移で _poolParent が Destroy されると Unity の null 判定が true になり、
        // 次回 EnsurePool で新しいプールが自動生成される。
        private static ObjectPool<EnemyProjectile> _pool;
        private static Transform _poolParent;

        public async UniTask Attack(EnemyController controller)
        {
            var data = controller.Data;
            EnsurePool(data);

            // チャージ中にプロジェクタイルを頭上に表示し、攻撃の予兆を伝える。
            // Launch 前は _isActive=false のため衝突判定・寿命チェックは走らない。
            var chargePosition = controller.transform.position + Vector3.up * ChargeHeightOffset;
            var projectile = _pool.Get();
            projectile.transform.SetPositionAndRotation(chargePosition, Quaternion.identity);

            GameObject chargeEffect = null;
            if (data.ChargeEffectPrefab != null)
            {
                chargeEffect = UnityEngine.Object.Instantiate(
                    data.ChargeEffectPrefab, controller.transform);
                chargeEffect.transform.localPosition = Vector3.up * ChargeHeightOffset;
            }

            bool cancelled = false;
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(data.ChargeTime),
                    cancellationToken: controller.destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                if (chargeEffect != null)
                {
                    UnityEngine.Object.Destroy(chargeEffect);
                }
            }

            // チャージ中に敵が破棄・死亡した場合、未発射のプロジェクタイルをプールに返却
            if (cancelled || controller.Model.IsDead)
            {
                _pool.Return(projectile);
                return;
            }

            // 発射の瞬間に攻撃アニメーションを再生する
            controller.PlayAnimation(EnemyController.AnimAttack);

            // チャージ位置からプレイヤーの目線に向かって放出する
            var targetPoint = controller.PlayerTransform.position
                            + Vector3.up * PlayerEyeHeightOffset;
            var direction = (targetPoint - projectile.transform.position).normalized;
            projectile.transform.rotation = Quaternion.LookRotation(direction);
            projectile.Launch(
                direction,
                data.ProjectileSpeed,
                data.AttackDamage,
                data.ProjectileMaxLifetime,
                data.ProjectileMaxRange,
                _pool);

            // 攻撃アニメーションの再生時間を確保してから AttackState に制御を返す。
            // これがないと AttackState が即座に AnimIdle で上書きしてしまう。
            await UniTask.Delay(
                TimeSpan.FromSeconds(AttackAnimationDuration),
                cancellationToken: controller.destroyCancellationToken);
        }

        /// <summary>
        /// プールが未作成またはシーン遷移で親オブジェクトが破棄された場合に再生成する。
        /// static フィールドのため、同シーン内の全 RangedAttack インスタンスが共有する。
        /// </summary>
        private static void EnsurePool(EnemyData data)
        {
            if (_poolParent != null) return;

            _poolParent = new GameObject("EnemyProjectilePool").transform;
            var prefabComponent = data.ProjectilePrefab.GetComponent<EnemyProjectile>();
            _pool = new ObjectPool<EnemyProjectile>(
                prefabComponent, _poolParent, PoolInitialSize, PoolMaxSize);
        }
    }
}
