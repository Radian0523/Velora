using System;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵の状態を一元管理する純粋な C# クラス。
    /// PlayerModel と同構造で、HP・スタッガー蓄積のロジックを MonoBehaviour から独立させ、
    /// テスト可能性とバランス調整の容易さを確保する。
    /// </summary>
    public class EnemyModel
    {
        private readonly float _maxHealth;
        private readonly float _staggerThreshold;
        private float _currentHealth;
        private float _accumulatedStaggerDamage;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _currentHealth <= 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamaged;
        public event Action OnStaggerTriggered;
        public event Action OnDeath;

        public EnemyModel(float maxHealth, float staggerThreshold)
        {
            _maxHealth = maxHealth;
            _staggerThreshold = staggerThreshold;
            _currentHealth = maxHealth;
        }

        /// <summary>
        /// ダメージを適用し、死亡・スタッガーの判定を行う。
        /// 死亡判定を優先し、死亡時はスタッガーを発火しない。
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnDamaged?.Invoke(amount);
            Debug.Log(OnHealthChanged == null ? "OnHealthChanged is null" : "OnHealthChanged has subscribers");
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
                return;
            }

            // スタッガー蓄積がしきい値を超えたら怯みを発火し、蓄積をリセット。
            // 死亡よりも優先度が低いため、死亡チェックの後に判定する。
            _accumulatedStaggerDamage += amount;
            if (_accumulatedStaggerDamage >= _staggerThreshold)
            {
                _accumulatedStaggerDamage = 0f;
                OnStaggerTriggered?.Invoke();
            }
        }

        /// <summary>
        /// ObjectPool 再利用時に全状態をリセットする。
        /// </summary>
        public void Reset()
        {
            _currentHealth = _maxHealth;
            _accumulatedStaggerDamage = 0f;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
