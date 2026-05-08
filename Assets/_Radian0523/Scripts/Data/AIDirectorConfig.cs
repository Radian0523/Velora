using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// AI Director のチューニングパラメータを Inspector で調整可能にする ScriptableObject。
    /// バランス調整がデータだけで完結するデータドリブン設計。
    /// パフォーマンス評価の重みや難易度スケーリングの範囲を変更するだけで、
    /// コード修正なしにゲーム体験を調整できる。
    /// </summary>
    [CreateAssetMenu(fileName = "AIDirectorConfig", menuName = "Velora/AI Director Config")]
    public class AIDirectorConfig : ScriptableObject
    {
        [Header("パフォーマンス評価")]
        [Tooltip("EMA（指数移動平均）の平滑化係数。高いほど直近のウェーブを重視する")]
        [SerializeField, Range(0.1f, 0.9f)] private float _smoothingFactor = 0.4f;

        [Tooltip("命中率の評価ウェイト")]
        [SerializeField, Range(0f, 1f)] private float _accuracyWeight = 0.30f;

        [Tooltip("クリア速度の評価ウェイト")]
        [SerializeField, Range(0f, 1f)] private float _clearSpeedWeight = 0.30f;

        [Tooltip("残り体力の評価ウェイト")]
        [SerializeField, Range(0f, 1f)] private float _healthWeight = 0.25f;

        [Tooltip("ヘッドショット率の評価ウェイト")]
        [SerializeField, Range(0f, 1f)] private float _headshotWeight = 0.15f;

        [Tooltip("基準クリア速度（秒/体）。この速度でクリアするとスピードスコアが 1.0")]
        [SerializeField] private float _expectedClearTimePerEnemy = 3.0f;

        [Header("難易度スケーリング")]
        [Tooltip("敵数の倍率範囲。x=低パフォーマンス時, y=高パフォーマンス時")]
        [SerializeField] private Vector2 _enemyCountScaleRange = new(0.7f, 1.5f);

        [Tooltip("スポーン間隔の倍率範囲。x=高パフォーマンス時(短い), y=低パフォーマンス時(長い)")]
        [SerializeField] private Vector2 _spawnDelayScaleRange = new(0.6f, 1.4f);

        [Tooltip("敵HPの倍率範囲。x=低パフォーマンス時, y=高パフォーマンス時")]
        [SerializeField] private Vector2 _healthScaleRange = new(0.8f, 1.3f);

        [Header("エンドレスモード")]
        [Tooltip("Wave 3 以降、ウェーブごとに追加される敵数")]
        [SerializeField] private int _baseGrowthPerWave = 1;

        [Tooltip("Ranged 敵の上限比率")]
        [SerializeField, Range(0f, 1f)] private float _maxRangedRatio = 0.5f;

        [Tooltip("エンドレスモードで使用する Ranged 敵データ")]
        [SerializeField] private EnemyData _rangedEnemyData;

        [Tooltip("エンドレスモードで使用する Rusher 敵データ")]
        [SerializeField] private EnemyData _rusherEnemyData;

        public float SmoothingFactor => _smoothingFactor;
        public float AccuracyWeight => _accuracyWeight;
        public float ClearSpeedWeight => _clearSpeedWeight;
        public float HealthWeight => _healthWeight;
        public float HeadshotWeight => _headshotWeight;
        public float ExpectedClearTimePerEnemy => _expectedClearTimePerEnemy;
        public Vector2 EnemyCountScaleRange => _enemyCountScaleRange;
        public Vector2 SpawnDelayScaleRange => _spawnDelayScaleRange;
        public Vector2 HealthScaleRange => _healthScaleRange;
        public int BaseGrowthPerWave => _baseGrowthPerWave;
        public float MaxRangedRatio => _maxRangedRatio;
        public EnemyData RangedEnemyData => _rangedEnemyData;
        public EnemyData RusherEnemyData => _rusherEnemyData;
    }
}
