using System;
using System.Collections.Generic;
using UnityEngine;
using Velora.Data;
using Velora.Player;

namespace Velora.Core
{
    /// <summary>
    /// プレイヤーのパフォーマンスに基づいて難易度を動的に調整する pure C# クラス。
    /// VContainer で Scoped 登録し、EventBus 経由でウェーブ単位のメトリクスを計測する。
    ///
    /// 設計意図:
    /// - パフォーマンス評価の重みや難易度スケーリングは AIDirectorConfig（ScriptableObject）で
    ///   管理し、コード変更なしにバランス調整できるデータドリブン設計とする。
    /// - EMA（指数移動平均）でスコアを平滑化し、1 ウェーブだけの突出した結果で
    ///   難易度が急変動するのを防ぐ。
    /// - Wave 1〜3 は既存の WaveData をベースに modifier を乗算、
    ///   Wave 4 以降は Wave 3 をテンプレートとしてエンドレス生成する。
    /// </summary>
    public class AIDirector : IDisposable
    {
        private readonly AIDirectorConfig _config;
        private readonly PlayerModel _playerModel;
        private readonly IReadOnlyList<WaveData> _baseWaves;

        // エンドレスモードの Ranged 比率初期値と増加量
        private const float EndlessBaseRangedRatio = 0.2f;
        private const float EndlessRangedGrowthPerWave = 0.05f;
        private const float EndlessBaseSpawnDelay = 0.3f;
        private const float EndlessDelayReductionPerWave = 0.05f;
        private const float EndlessMinDelayFactor = 0.1f;
        private const float EndlessHealthGrowthPerWave = 0.1f;

        private int _shotsFired;
        private int _hitsLanded;
        private int _headshots;
        private float _damageTaken;
        private float _waveStartTime;
        private float _waveClearTime;
        private int _waveEnemyCount;

        private float _smoothedPerformance = 0.5f;
        private bool _isDisposed;

        /// <summary>
        /// EMA で平滑化されたパフォーマンススコア（0.0〜1.0）。
        /// デバッグ HUD や DifficultyAdjustedEvent で参照可能。
        /// </summary>
        public float SmoothedPerformance => _smoothedPerformance;

        public AIDirector(
            AIDirectorConfig config,
            PlayerModel playerModel,
            IReadOnlyList<WaveData> baseWaves)
        {
            _config = config;
            _playerModel = playerModel;
            _baseWaves = baseWaves;

            // Wave 1 の敵数を初期値として設定（BuildNextWaveConfig を経由しないため）
            _waveEnemyCount = _baseWaves.Count > 0 ? _baseWaves[0].TotalEnemyCount : 1;

            EventBus.Subscribe<WeaponFiredEvent>(HandleWeaponFired);
            EventBus.Subscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Subscribe<WaveStartedEvent>(HandleWaveStarted);
            EventBus.Subscribe<WaveClearedEvent>(HandleWaveCleared);
        }

        /// <summary>
        /// 次ウェーブの構成を生成する。
        /// 直前のウェーブメトリクスからパフォーマンスを算出し、
        /// base wave があればそれを元に modifier を適用、なければエンドレス生成する。
        /// ResetWaveMetrics の前に呼ぶこと（メトリクスを参照するため）。
        /// </summary>
        public RuntimeWaveConfig BuildNextWaveConfig(int waveIndex)
        {
            float performance = CalculateAndUpdatePerformance();

            RuntimeWaveConfig config;
            if (waveIndex < _baseWaves.Count)
            {
                config = BuildModifiedBaseWave(_baseWaves[waveIndex], performance);
            }
            else
            {
                config = BuildEndlessWave(waveIndex, performance);
            }

            _waveEnemyCount = config.TotalEnemyCount;
            return config;
        }

        /// <summary>
        /// ウェーブ開始前にメトリクスをリセットする。
        /// BuildNextWaveConfig でメトリクスを使い終えた後に呼ぶ。
        /// </summary>
        public void ResetWaveMetrics()
        {
            _shotsFired = 0;
            _hitsLanded = 0;
            _headshots = 0;
            _damageTaken = 0f;
        }

        public bool IsEndlessPhase(int waveIndex) => waveIndex >= _baseWaves.Count;

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            EventBus.Unsubscribe<WeaponFiredEvent>(HandleWeaponFired);
            EventBus.Unsubscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<WaveStartedEvent>(HandleWaveStarted);
            EventBus.Unsubscribe<WaveClearedEvent>(HandleWaveCleared);
        }

        // --- パフォーマンス計算 ---

        /// <summary>
        /// 直前のウェーブメトリクスから 4 指標の加重平均を算出し、
        /// EMA で平滑化する。各指標は 0〜1 に正規化され、
        /// 基準値（命中率 70%、ヘッドショット率 30%）を超えると 1.0 に飽和する。
        /// </summary>
        private float CalculateAndUpdatePerformance()
        {
            float accuracy = _shotsFired > 0 ? (float)_hitsLanded / _shotsFired : 0.5f;
            float headshotRatio = _hitsLanded > 0 ? (float)_headshots / _hitsLanded : 0f;

            float accuracyScore = Mathf.Clamp01(accuracy / 0.70f);

            float expectedTime = _waveEnemyCount * _config.ExpectedClearTimePerEnemy;
            float speedScore = _waveClearTime > 0f
                ? Mathf.Clamp01(expectedTime / _waveClearTime)
                : 0.5f;

            float healthScore = _playerModel.MaxHealth > 0f
                ? _playerModel.CurrentHealth / _playerModel.MaxHealth
                : 0f;

            float headshotScore = Mathf.Clamp01(headshotRatio / 0.30f);

            float wavePerformance =
                accuracyScore * _config.AccuracyWeight +
                speedScore * _config.ClearSpeedWeight +
                healthScore * _config.HealthWeight +
                headshotScore * _config.HeadshotWeight;

            _smoothedPerformance = Mathf.Lerp(
                _smoothedPerformance, wavePerformance, _config.SmoothingFactor);

            return _smoothedPerformance;
        }

        // --- ウェーブ構成生成 ---

        /// <summary>
        /// base wave（Wave 1〜3）にパフォーマンスベースの modifier を乗算する。
        /// 敵数・スポーン間隔・HP をスケーリングし、プレイヤーの腕に応じた体験を提供する。
        /// </summary>
        private RuntimeWaveConfig BuildModifiedBaseWave(WaveData baseWave, float performance)
        {
            float enemyCountScale = Mathf.Lerp(
                _config.EnemyCountScaleRange.x, _config.EnemyCountScaleRange.y, performance);
            float spawnDelayScale = Mathf.Lerp(
                _config.SpawnDelayScaleRange.y, _config.SpawnDelayScaleRange.x, performance);
            float healthScale = Mathf.Lerp(
                _config.HealthScaleRange.x, _config.HealthScaleRange.y, performance);

            var entries = new List<RuntimeSpawnEntry>();
            foreach (var entry in baseWave.SpawnEntries)
            {
                int adjustedCount = Mathf.Max(1, Mathf.RoundToInt(entry.Count * enemyCountScale));
                float adjustedDelay = entry.SpawnDelay * spawnDelayScale;
                entries.Add(new RuntimeSpawnEntry(entry.EnemyData, adjustedCount, adjustedDelay));
            }

            EventBus.Publish(new DifficultyAdjustedEvent(
                performance, enemyCountScale, healthScale, baseWave.WaveNumber));

            return new RuntimeWaveConfig(baseWave.WaveNumber, entries, healthScale);
        }

        /// <summary>
        /// Wave 4 以降のエンドレスウェーブを生成する。
        /// Wave 3（最終 base wave）をテンプレートとし、ウェーブ番号に応じてスケーリングする。
        /// Ranged 比率を徐々に上昇させ、後半ほど戦略的なプレイを要求する。
        /// パフォーマンスベースの modifier をさらに乗算し、難易度が一方的に上昇しないようにする。
        /// </summary>
        private RuntimeWaveConfig BuildEndlessWave(int waveIndex, float performance)
        {
            var templateWave = _baseWaves[_baseWaves.Count - 1];
            int waveNumber = waveIndex + 1;
            int wavesIntoEndless = waveIndex - _baseWaves.Count + 1;

            // 敵数: テンプレートの総数 + ウェーブ進行による増加 × パフォーマンス modifier
            int baseEnemyCount = templateWave.TotalEnemyCount
                + wavesIntoEndless * _config.BaseGrowthPerWave;
            float enemyCountScale = Mathf.Lerp(
                _config.EnemyCountScaleRange.x, _config.EnemyCountScaleRange.y, performance);
            int totalEnemies = Mathf.Max(1, Mathf.RoundToInt(baseEnemyCount * enemyCountScale));

            // Ranged 比率: ウェーブが進むほど上昇（上限あり）
            float rangedRatio = Mathf.Min(
                _config.MaxRangedRatio,
                EndlessBaseRangedRatio + wavesIntoEndless * EndlessRangedGrowthPerWave);
            int rangedCount = Mathf.RoundToInt(totalEnemies * rangedRatio);
            int rusherCount = totalEnemies - rangedCount;

            // スポーン間隔: ウェーブ進行で短縮 × パフォーマンス modifier
            float spawnDelayScale = Mathf.Lerp(
                _config.SpawnDelayScaleRange.y, _config.SpawnDelayScaleRange.x, performance);
            float waveDelayFactor = Mathf.Max(
                EndlessMinDelayFactor,
                1f - wavesIntoEndless * EndlessDelayReductionPerWave);
            float finalDelay = EndlessBaseSpawnDelay * spawnDelayScale * waveDelayFactor;

            // HP マルチプライヤー: ウェーブ進行で上昇 × パフォーマンス modifier
            float baseHealthMultiplier = 1f + wavesIntoEndless * EndlessHealthGrowthPerWave;
            float healthScale = Mathf.Lerp(
                _config.HealthScaleRange.x, _config.HealthScaleRange.y, performance);
            float finalHealthMultiplier = baseHealthMultiplier * healthScale;

            var entries = new List<RuntimeSpawnEntry>();
            if (rusherCount > 0)
            {
                entries.Add(new RuntimeSpawnEntry(_config.RusherEnemyData, rusherCount, finalDelay));
            }
            if (rangedCount > 0)
            {
                entries.Add(new RuntimeSpawnEntry(_config.RangedEnemyData, rangedCount, finalDelay));
            }

            EventBus.Publish(new DifficultyAdjustedEvent(
                performance, enemyCountScale, healthScale, waveNumber));

            return new RuntimeWaveConfig(waveNumber, entries, finalHealthMultiplier);
        }

        // --- EventBus ハンドラ ---

        private void HandleWeaponFired(WeaponFiredEvent e) => _shotsFired++;

        private void HandleEnemyDamaged(EnemyDamagedEvent e)
        {
            _hitsLanded++;
            if (e.IsHeadshot) _headshots++;
        }

        private void HandlePlayerDamaged(PlayerDamagedEvent e) => _damageTaken += e.Damage;

        private void HandleWaveStarted(WaveStartedEvent e) => _waveStartTime = Time.time;

        private void HandleWaveCleared(WaveClearedEvent e) =>
            _waveClearTime = Time.time - _waveStartTime;
    }
}
