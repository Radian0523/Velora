using System.Collections.Generic;
using UnityEngine;
using Velora.Core;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// エフェクトプールのライフサイクルとスポーンを管理する純粋 C# クラス。
    /// プレハブ単位でプールをキャッシュし、同じエフェクトなら武器間で共有する。
    /// 武器切替時に Destroy しないため、飛行中のプロジェクタイルが参照するプールが
    /// 破棄される問題を回避する。
    /// </summary>
    public class WeaponEffectPoolManager
    {
        private ObjectPool<PooledEffect> _impactEffectPool;
        private ObjectPool<PooledEffect> _explosionEffectPool;
        private readonly Dictionary<GameObject, ObjectPool<PooledEffect>> _effectPoolCache = new();

        private const int EffectPoolInitialSize = 3;
        private const int EffectPoolMaxSize = 10;

        public ObjectPool<PooledEffect> ImpactEffectPool => _impactEffectPool;
        public ObjectPool<PooledEffect> ExplosionEffectPool => _explosionEffectPool;

        /// <summary>
        /// 現在の武器に必要なエフェクトプールを取得する。
        /// 同じプレハブのプールは武器間で共有し、生成済みなら使い回す。
        /// </summary>
        public void InitializeForWeapon(WeaponData weaponData)
        {
            _impactEffectPool = GetOrCreateEffectPool(weaponData.ImpactEffectPrefab);
            _explosionEffectPool = GetOrCreateEffectPool(weaponData.ExplosionEffectPrefab);
        }

        public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_impactEffectPool == null) return;

            var effect = _impactEffectPool.Get();
            effect.Initialize(_impactEffectPool);
            effect.transform.SetPositionAndRotation(hitPoint, Quaternion.LookRotation(hitNormal));
        }

        /// <summary>
        /// 全エフェクトプールを破棄する。シーン遷移時のクリーンアップに使用。
        /// </summary>
        public void Cleanup()
        {
            foreach (var pool in _effectPoolCache.Values)
            {
                pool.Clear();
            }
            _effectPoolCache.Clear();
            _impactEffectPool = null;
            _explosionEffectPool = null;
        }

        private ObjectPool<PooledEffect> GetOrCreateEffectPool(GameObject prefab)
        {
            if (prefab == null) return null;

            if (_effectPoolCache.TryGetValue(prefab, out var cached))
            {
                return cached;
            }

            var pooledEffect = prefab.GetComponent<PooledEffect>();
            if (pooledEffect == null) return null;

            var poolParent = new GameObject($"Pool_{prefab.name}").transform;
            var pool = new ObjectPool<PooledEffect>(
                pooledEffect, poolParent, EffectPoolInitialSize, EffectPoolMaxSize);

            for (int i = 0; i < EffectPoolInitialSize; i++)
            {
                var instance = pool.Get();
                instance.Initialize(pool);
                pool.Return(instance);
            }

            _effectPoolCache[prefab] = pool;
            return pool;
        }
    }
}
