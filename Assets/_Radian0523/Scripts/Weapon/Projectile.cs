using System.Collections.Generic;
using UnityEngine;
using Velora.Battle;
using Velora.Core;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 弾丸の飛行・衝突処理を担当する MonoBehaviour。
    /// ProjectileStrategy から ObjectPool 経由で取得され、
    /// 衝突または生存時間超過でプールに返却される。
    /// 着弾エフェクトは PooledEffect プールから取得し、返却は PooledEffect が自動処理する。
    ///
    /// スプラッシュダメージ対応:
    /// WeaponData.HasSplashDamage が有効な場合、着弾地点を中心に
    /// OverlapSphere で範囲内の IDamageable を収集し、距離減衰付きダメージを適用する。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private Collider _projectileCollider;
        private float _spawnTime;
        private float _maxLifetime;
        private LayerMask _hitMask;
        private WeaponData _weaponData;
        private float _damageMultiplier;
        private ObjectPool<Projectile> _pool;
        private ObjectPool<PooledEffect> _impactEffectPool;
        private ObjectPool<PooledEffect> _explosionEffectPool;
        private bool _isActive;
        private Collider _ownerCollider;

        // OverlapSphere のバッファを static で確保し、GC Alloc を回避する
        private static readonly Collider[] SplashBuffer = new Collider[32];
        private static readonly HashSet<IDamageable> ProcessedTargets = new();

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _projectileCollider = GetComponent<Collider>();
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Launch(
            float speed,
            LayerMask hitMask,
            WeaponData weaponData,
            float damageMultiplier,
            ObjectPool<Projectile> pool,
            ObjectPool<PooledEffect> impactEffectPool,
            ObjectPool<PooledEffect> explosionEffectPool,
            Collider ownerCollider = null)
        {
            _hitMask = hitMask;
            _weaponData = weaponData;
            _damageMultiplier = damageMultiplier;
            _maxLifetime = weaponData.ProjectileMaxLifetime;
            _pool = pool;
            _impactEffectPool = impactEffectPool;
            _explosionEffectPool = explosionEffectPool;
            _spawnTime = Time.time;
            _isActive = true;

            // 発射元のコライダーとの衝突を無視する。
            // カメラ位置（プレイヤーの頭部内）から弾が生成されるため、
            // IgnoreCollision を使わないと即座に自分自身に当たってしまう。
            SetOwnerCollision(ownerCollider);

            _rigidbody.useGravity = weaponData.ProjectileUseGravity;
            _rigidbody.linearVelocity = transform.forward * speed;
        }

        private void SetOwnerCollision(Collider newOwner)
        {
            // 前回の発射元との無視設定を解除してからプール再利用に備える
            if (_ownerCollider != null && _projectileCollider != null)
            {
                Physics.IgnoreCollision(_projectileCollider, _ownerCollider, false);
            }

            _ownerCollider = newOwner;

            if (_ownerCollider != null && _projectileCollider != null)
            {
                Physics.IgnoreCollision(_projectileCollider, _ownerCollider, true);
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            if (Time.time - _spawnTime >= _maxLifetime)
            {
                // 生存時間切れでもスプラッシュダメージと爆発エフェクトを発生させる。
                // ロケットランチャーの弾が地面に落ちきる前に消えても爆発する挙動を再現する。
                if (_weaponData.HasSplashDamage)
                {
                    ApplySplashDamage(transform.position, directHitTarget: null);
                    SpawnExplosionEffect(transform.position);
                }

                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            if ((_hitMask.value & (1 << collision.gameObject.layer)) == 0) return;

            var contact = collision.GetContact(0);
            IDamageable directHitTarget = null;

            if (collision.gameObject.GetComponentInParent<IDamageable>() is IDamageable damageable)
            {
                damageable.TakeDamage(_weaponData.Damage * _damageMultiplier, contact.point, false);
                directHitTarget = damageable;
            }

            if (_weaponData.HasSplashDamage)
            {
                ApplySplashDamage(contact.point, directHitTarget);
                SpawnExplosionEffect(contact.point);
            }
            else
            {
                SpawnImpactEffect(collision);
            }

            ReturnToPool();
        }

        /// <summary>
        /// 着弾地点を中心にスプラッシュダメージを適用する。
        /// Physics.OverlapSphereNonAlloc で範囲内の Collider を収集し、
        /// Collider.ClosestPoint で正確な距離を算出して SplashFalloff で減衰させる。
        /// 直撃した対象は二重ダメージを防ぐため除外する。
        /// </summary>
        private void ApplySplashDamage(Vector3 center, IDamageable directHitTarget)
        {
            ProcessedTargets.Clear();

            if (directHitTarget != null)
            {
                ProcessedTargets.Add(directHitTarget);
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                center, _weaponData.SplashRadius, SplashBuffer, _hitMask);

            float baseDamage = _weaponData.Damage * _damageMultiplier * _weaponData.SplashDamageMultiplier;

            for (int i = 0; i < hitCount; i++)
            {
                var col = SplashBuffer[i];
                if (col == null) continue;

                // CanSelfDamage が無効の場合、Player レイヤーへのダメージをスキップする
                if (!_weaponData.CanSelfDamage && col.CompareTag("Player")) continue;

                if (col.GetComponentInParent<IDamageable>() is not IDamageable target) continue;
                if (ProcessedTargets.Contains(target)) continue;
                ProcessedTargets.Add(target);

                // ClosestPoint で正確な距離を算出し、AnimationCurve で減衰を適用する。
                // 中心に近いほど高ダメージ、外縁ではほぼ 0 になる。
                Vector3 closestPoint = col.ClosestPoint(center);
                float distance = Vector3.Distance(center, closestPoint);
                float normalizedDistance = Mathf.Clamp01(distance / _weaponData.SplashRadius);
                float falloff = _weaponData.SplashFalloff.Evaluate(normalizedDistance);

                float splashDamage = baseDamage * falloff;
                if (splashDamage > 0f)
                {
                    target.TakeDamage(splashDamage, closestPoint, false);
                }
            }

            ProcessedTargets.Clear();
        }

        private void SpawnExplosionEffect(Vector3 position)
        {
            if (_explosionEffectPool == null) return;

            var effect = _explosionEffectPool.Get();
            effect.Initialize(_explosionEffectPool);
            effect.transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        private void SpawnImpactEffect(Collision collision)
        {
            if (_impactEffectPool == null) return;

            var contact = collision.GetContact(0);
            var effect = _impactEffectPool.Get();
            effect.transform.SetPositionAndRotation(
                contact.point,
                Quaternion.LookRotation(contact.normal));
        }

        private void ReturnToPool()
        {
            _isActive = false;
            _rigidbody.useGravity = false;
            _rigidbody.linearVelocity = Vector3.zero;
            SetOwnerCollision(null);
            _pool.Return(this);
        }
    }
}
