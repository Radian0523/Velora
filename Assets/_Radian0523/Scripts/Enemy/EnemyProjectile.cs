using UnityEngine;
using Velora.Battle;
using Velora.Core;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵の遠距離攻撃用プロジェクタイル。
    /// Projectile（プレイヤー武器用）と同じオブジェクトプールパターンで管理される。
    /// isTrigger コライダーで飛行し、EnemyController を持つオブジェクトは貫通、
    /// IDamageable を持つ非敵オブジェクト（プレイヤー）にはダメージを適用する。
    /// 最大射程を超えるとプールに返却され、寿命は異常時のフォールバックとして残す。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyProjectile : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private ObjectPool<EnemyProjectile> _pool;
        private float _damage;
        private float _spawnTime;
        private float _maxLifetime;
        private float _maxRangeSqr;
        private Vector3 _spawnPosition;
        private bool _isActive;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Launch(
            Vector3 direction,
            float speed,
            float damage,
            float maxLifetime,
            float maxRange,
            ObjectPool<EnemyProjectile> pool)
        {
            _damage = damage;
            _maxLifetime = maxLifetime;
            _maxRangeSqr = maxRange * maxRange;
            _pool = pool;
            _spawnTime = Time.time;
            _spawnPosition = transform.position;
            _isActive = true;

            _rigidbody.linearVelocity = direction * speed;
        }

        private void Update()
        {
            if (!_isActive) return;

            if ((transform.position - _spawnPosition).sqrMagnitude >= _maxRangeSqr)
            {
                ReturnToPool();
                return;
            }

            // 寿命超過のフォールバック（射程内で滞留する異常ケース対策）
            if (Time.time - _spawnTime >= _maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            // 敵同士の味方撃ちを防止する（EnemyController を持つ階層はすべて貫通）
            if (other.GetComponentInParent<EnemyController>() != null) return;

            if (other.GetComponentInParent<IDamageable>() is IDamageable damageable)
            {
                damageable.TakeDamage(_damage, transform.position, false);
            }

            // IDamageable の有無に関わらず、敵以外に衝突したらプールに返却する（壁・床を含む）
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _isActive = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _pool.Return(this);
        }
    }
}
