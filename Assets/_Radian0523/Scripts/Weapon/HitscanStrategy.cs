using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Battle;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// レイキャスト即着弾方式の射撃ストラテジー。
    /// アサルトライフルやピストルなど弾速が無限の武器に使用する。
    /// 拡散角に基づくランダムオフセットでレイの方向をばらつかせる。
    /// エフェクト生成は行わず、ヒット情報を FireResult で返す。
    /// </summary>
    public class HitscanStrategy : IFireStrategy
    {
        private const float MaxRayDistance = 200f;

        public UniTask<FireResult> Fire(WeaponData data, Transform muzzle, LayerMask hitMask, float spreadAngle)
        {
            var direction = ApplySpread(muzzle.forward, spreadAngle);

            // TODO: カメラの位置から、カメラの方向にraycastするようにする。crosshairの部分にちゃんと当たるため
            // QueryTriggerInteraction.Collide でヘッドショット用の Trigger Collider もヒット対象にする
            if (Physics.Raycast(muzzle.position, direction, out var hit, MaxRayDistance, hitMask,
                QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    bool isHeadshot = hit.collider.CompareTag("Headshot");
                    float damage = data.Damage * (isHeadshot ? data.HeadshotMultiplier : 1f);
                    damageable.TakeDamage(damage, hit.point, isHeadshot);
                }

                return UniTask.FromResult(new FireResult(true, hit.point, hit.normal));
            }

            return UniTask.FromResult(FireResult.None);
        }

        /// <summary>
        /// 射撃方向に拡散角を適用する。
        /// 円錐状の一様分布でランダムな方向をサンプリングし、自然な弾のばらつきを再現。
        /// </summary>
        private Vector3 ApplySpread(Vector3 forward, float spreadAngle)
        {
            if (spreadAngle <= 0f) return forward;

            float halfAngleRad = spreadAngle * 0.5f * Mathf.Deg2Rad;
            float randomAngle = Random.Range(0f, 2f * Mathf.PI);
            float randomRadius = Mathf.Tan(halfAngleRad) * Random.Range(0f, 1f);

            var right = Vector3.Cross(forward, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(forward, Vector3.right).normalized;
            }
            var up = Vector3.Cross(right, forward);

            var offset = (right * Mathf.Cos(randomAngle) + up * Mathf.Sin(randomAngle)) * randomRadius;
            return (forward + offset).normalized;
        }
    }
}
