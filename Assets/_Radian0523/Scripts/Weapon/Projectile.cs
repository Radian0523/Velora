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
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        private const float MaxLifetime = 5f;

        private Rigidbody _rigidbody;
        private float _spawnTime;
        private LayerMask _hitMask;
        private WeaponData _weaponData;
        private ObjectPool<Projectile> _pool;
        private ObjectPool<PooledEffect> _impactEffectPool;
        private bool _isActive;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Launch(
            float speed,
            LayerMask hitMask,
            WeaponData weaponData,
            ObjectPool<Projectile> pool,
            ObjectPool<PooledEffect> impactEffectPool)
        {
            _hitMask = hitMask;
            _weaponData = weaponData;
            _pool = pool;
            _impactEffectPool = impactEffectPool;
            _spawnTime = Time.time;
            _isActive = true;

            _rigidbody.linearVelocity = transform.forward * speed;
        }

        private void Update()
        {
            if (!_isActive) return;

            if (Time.time - _spawnTime >= MaxLifetime)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isActive) return;

            // hitMask に含まれないレイヤーは無視する
            if ((_hitMask.value & (1 << collision.gameObject.layer)) == 0) return;

            if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
            {
                var contact = collision.GetContact(0);
                damageable.TakeDamage(_weaponData.Damage, contact.point, false);
            }

            SpawnImpactEffect(collision);
            ReturnToPool();
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
            _rigidbody.linearVelocity = Vector3.zero;
            _pool.Return(this);
        }
    }
}
