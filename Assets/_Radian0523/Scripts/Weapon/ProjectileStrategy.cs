using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Core;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 弾丸飛行方式の射撃ストラテジー。
    /// ロケットランチャーやグレネードなど、弾丸が物理的に飛行する武器に使用する。
    /// ObjectPool で弾丸を再利用し、Instantiate/Destroy のコストを回避する。
    /// 着弾エフェクトは Projectile が非同期で衝突時に処理するため、常に FireResult.None を返す。
    /// </summary>
    public class ProjectileStrategy : IFireStrategy
    {
        private ObjectPool<Projectile> _pool;
        private ObjectPool<PooledEffect> _impactEffectPool;
        private WeaponData _cachedData;

        private const int PoolInitialSize = 5;
        private const int PoolMaxSize = 20;

        public ProjectileStrategy(ObjectPool<PooledEffect> impactEffectPool)
        {
            _impactEffectPool = impactEffectPool;
        }

        public UniTask<FireResult> Fire(WeaponData data, Transform origin, LayerMask hitMask, float spreadAngle)
        {
            EnsurePool(data);

            var projectile = _pool.Get();
            projectile.transform.SetPositionAndRotation(origin.position, origin.rotation);
            projectile.Launch(data.ProjectileSpeed, hitMask, data, _pool, _impactEffectPool);

            return UniTask.FromResult(FireResult.None);
        }

        /// <summary>
        /// WeaponData が変わった場合のみプールを再生成する。
        /// 同じ武器で連射する限りプールを使い回す。
        /// </summary>
        private void EnsurePool(WeaponData data)
        {
            if (_pool != null && _cachedData == data) return;

            _pool?.Clear();

            var prefab = data.ProjectilePrefab;
            var poolParent = new GameObject($"Pool_{data.WeaponName}_Projectiles").transform;

            // Prefab には Projectile コンポーネントが事前に設定されている前提。
            // 未設定の場合は Inspector 側の設定ミスなので即座にエラーで検出する。
            var projectileComponent = prefab.GetComponent<Projectile>();

            _pool = new ObjectPool<Projectile>(projectileComponent, poolParent, PoolInitialSize, PoolMaxSize);
            _cachedData = data;
        }
    }
}
