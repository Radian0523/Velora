using UnityEngine;
using Cysharp.Threading.Tasks;
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
        private ObjectPool<PooledEffect> _explosionEffectPool;
        private Collider _ownerCollider;
        private WeaponData _cachedData;

        private const int PoolInitialSize = 5;
        private const int PoolMaxSize = 20;

        public ProjectileStrategy(
            ObjectPool<PooledEffect> impactEffectPool,
            ObjectPool<PooledEffect> explosionEffectPool,
            Collider ownerCollider)
        {
            _impactEffectPool = impactEffectPool;
            _explosionEffectPool = explosionEffectPool;
            _ownerCollider = ownerCollider;
        }

        public UniTask<FireResult> Fire(WeaponData data, Transform origin, LayerMask hitMask, float spreadAngle, float damageMultiplier)
        {
            EnsurePool(data);

            // EnsurePool が設定ミスでプール生成を中断した場合は、発射せず安全に抜ける。
            if (_pool == null)
            {
                return UniTask.FromResult(FireResult.None);
            }

            var projectile = _pool.Get();
            projectile.transform.SetPositionAndRotation(origin.position, origin.rotation);
            projectile.Launch(
                data.ProjectileSpeed, hitMask, data, damageMultiplier,
                _pool, _impactEffectPool, _explosionEffectPool, _ownerCollider);

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

            // Prefab には Projectile コンポーネントが事前に設定されている前提。
            // 未設定の場合は Inspector 側の設定ミスなので、プールを生成せず即座にエラーで検出する。
            // ここで弾くことで、後続の Get() 時に NullReferenceException で原因が分かりにくくなるのを防ぐ。
            if (prefab == null || prefab.GetComponent<Projectile>() == null)
            {
                Debug.LogError(
                    $"[{nameof(ProjectileStrategy)}] {data.WeaponName} の ProjectilePrefab に "
                    + $"{nameof(Projectile)} コンポーネントが設定されていません。WeaponData を確認してください。");
                return;
            }

            var poolParent = new GameObject($"Pool_{data.WeaponName}_Projectiles").transform;
            var projectileComponent = prefab.GetComponent<Projectile>();

            _pool = new ObjectPool<Projectile>(projectileComponent, poolParent, PoolInitialSize, PoolMaxSize);
            _cachedData = data;
        }
    }
}
