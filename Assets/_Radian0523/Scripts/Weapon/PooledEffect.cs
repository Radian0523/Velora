using UnityEngine;
using Velora.Core;

namespace Velora.Weapon
{
    /// <summary>
    /// エフェクトプレハブにアタッチし、パーティクル終了時にプールへ自動返却する。
    /// ParticleSystem.IsAlive() を監視するため、Destroy(obj, 2f) のような
    /// 固定時間ではなくパーティクル設定に寿命管理を委ねられる。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledEffect : MonoBehaviour
    {
        private ParticleSystem _particleSystem;
        private ObjectPool<PooledEffect> _pool;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        public void Initialize(ObjectPool<PooledEffect> pool)
        {
            _pool = pool;
        }

        private void OnEnable()
        {
            _particleSystem.Play();
        }

        private void Update()
        {
            if (_pool == null) return;

            if (!_particleSystem.IsAlive())
            {
                _pool.Return(this);
            }
        }
    }
}
