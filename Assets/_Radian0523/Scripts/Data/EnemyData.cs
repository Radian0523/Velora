using UnityEngine;

namespace Velora.Data
{
    public enum EnemyBehaviorType
    {
        Rusher,
        Ranged
    }

    /// <summary>
    /// 敵パラメータの ScriptableObject。
    /// 新しい敵タイプは Inspector でこの SO を1つ作るだけで追加できる。
    /// BehaviorType に応じて攻撃方式が自動選択されるため、コード変更は不要。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Velora/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本情報")]
        [SerializeField] private string _enemyName;
        [SerializeField] private EnemyBehaviorType _behaviorType;

        [Header("ステータス")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _moveSpeed = 3.5f;

        [Header("攻撃")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _detectionRange = 20f;
        [SerializeField] private float _attackCooldown = 1.5f;

        [Header("スタッガー")]
        [SerializeField] private float _staggerThreshold = 30f;
        [SerializeField] private float _staggerDuration = 0.5f;

        [Header("Ranged 専用")]
        [SerializeField] private float _preferredRange = 12f;
        [SerializeField] private float _minRetreatRange = 5f;
        [SerializeField] private float _chargeTime = 0.3f;
        [SerializeField] private float _projectileSpeed = 15f;
        [SerializeField] private float _projectileMaxLifetime = 5f;
        [SerializeField] private float _projectileMaxRange = 30f;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _chargeEffectPrefab;

        [Header("外見")]
        [Tooltip("T_ColorAtlas の列（0〜2）。色系統を切り替える")]
        [SerializeField, Range(0, 2)] private int _colorColumn;
        [Tooltip("T_ColorAtlas の行（0〜31）。色合いを切り替える")]
        [SerializeField, Range(0, 31)] private int _colorRow;

        [Header("エフェクト")]
        [SerializeField] private GameObject _deathEffectPrefab;
        [SerializeField] private GameObject _spawnEffectPrefab;

        [Header("スコア")]
        [SerializeField] private float _spawnWeight = 1f;
        [SerializeField] private int _scoreValue = 100;

        public string EnemyName => _enemyName;
        public EnemyBehaviorType BehaviorType => _behaviorType;
        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float DetectionRange => _detectionRange;
        public float AttackCooldown => _attackCooldown;
        public float StaggerThreshold => _staggerThreshold;
        public float StaggerDuration => _staggerDuration;
        public float PreferredRange => _preferredRange;
        public float MinRetreatRange => _minRetreatRange;
        public float ChargeTime => _chargeTime;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileMaxLifetime => _projectileMaxLifetime;
        public float ProjectileMaxRange => _projectileMaxRange;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public GameObject ChargeEffectPrefab => _chargeEffectPrefab;
        public int ColorColumn => _colorColumn;
        public int ColorRow => _colorRow;
        public GameObject DeathEffectPrefab => _deathEffectPrefab;
        public GameObject SpawnEffectPrefab => _spawnEffectPrefab;
        public float SpawnWeight => _spawnWeight;
        public int ScoreValue => _scoreValue;
    }
}
