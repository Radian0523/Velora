using System;
using System.Collections.Generic;
using UnityEngine;
using Velora.Data;

namespace Velora.Player
{
    /// <summary>
    /// プレイヤーの状態を一元管理する純粋な C# クラス。
    /// FPSController（物理・入力）と分離し、ロジック層を独立させることで
    /// 戦闘バランスの調整時に物理やUI側の変更を不要にする。
    /// </summary>
    public class PlayerModel
    {
        private float _maxHealth;
        private float _currentHealth;
        private readonly List<UpgradeData> _appliedUpgrades = new();

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _currentHealth <= 0f;
        public IReadOnlyList<UpgradeData> AppliedUpgrades => _appliedUpgrades;

        // アップグレードで変動するバフ倍率
        public float DamageMultiplier { get; private set; } = 1f;
        public float FireRateMultiplier { get; private set; } = 1f;
        public float ReloadSpeedMultiplier { get; private set; } = 1f;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<float> OnDamaged;
        public event Action<UpgradeData> OnUpgradeApplied;

        public PlayerModel(float maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnDamaged?.Invoke(amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// アップグレードを適用する。UpgradeType に応じて対応する倍率を更新する。
        /// </summary>
        public void ApplyUpgrade(UpgradeData upgrade)
        {
            _appliedUpgrades.Add(upgrade);

            switch (upgrade.UpgradeType)
            {
                case UpgradeType.DamageBoost:
                    DamageMultiplier += upgrade.EffectValue;
                    break;
                case UpgradeType.FireRateBoost:
                    FireRateMultiplier += upgrade.EffectValue;
                    break;
                case UpgradeType.ReloadSpeedBoost:
                    ReloadSpeedMultiplier += upgrade.EffectValue;
                    break;
                case UpgradeType.MaxHealthBoost:
                    _maxHealth += upgrade.EffectValue;
                    _currentHealth += upgrade.EffectValue;
                    OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                    break;
                case UpgradeType.HealNow:
                    Heal(upgrade.EffectValue);
                    break;
            }

            OnUpgradeApplied?.Invoke(upgrade);
        }

        /// <summary>
        /// ゲーム開始時の初期化。全状態をリセットする。
        /// </summary>
        public void Reset(float maxHealth)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            _appliedUpgrades.Clear();
            DamageMultiplier = 1f;
            FireRateMultiplier = 1f;
            ReloadSpeedMultiplier = 1f;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
