using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Velora.Battle;
using Velora.Core;
using Velora.Data;
using Velora.Enemy;

namespace Velora.Wave
{
    /// <summary>
    /// ウェーブの進行を管理する pure C# クラス。
    /// ObjectPool で敵を再利用し、EventBus 経由で敵の死亡を監視して
    /// ウェーブクリア判定を行う。MonoBehaviour に依存しないため、
    /// ロジックのテストやバランス調整が容易になる。
    ///
    /// AI Director 統合:
    /// SetPendingWaveConfig で RuntimeWaveConfig を事前にセットしておくと、
    /// 次の StartWave 呼び出し時にその構成でスポーンする。
    /// これにより BattleInProgressState の変更なしに動的ウェーブ生成を実現する。
    /// </summary>
    public class WaveDirector : IDisposable
    {
        private readonly IReadOnlyList<WaveData> _waveDataList;
        private readonly SpawnPointManager _spawnPointManager;
        private readonly Transform _playerTransform;
        private readonly IDamageable _playerDamageable;
        private readonly ObjectPool<EnemyController> _enemyPool;

        private int _currentWaveIndex;
        private int _activeEnemyCount;
        private int _activeWaveNumber;
        private RuntimeWaveConfig _pendingConfig;
        private bool _isDisposed;

        /// <summary>
        /// 現在のウェーブ番号。BattleReadyState / WaveClearedState が演出表示に使用する。
        /// SetPendingWaveConfig で事前に更新されるため、BattleReady 遷移時には正しい値が入る。
        /// </summary>
        public int CurrentWaveNumber => _activeWaveNumber;

        public bool HasNextWave => _currentWaveIndex + 1 < _waveDataList.Count;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCleared;
        public event Action OnAllWavesComplete;

        private const int PoolInitialSize = 10;
        private const int PoolMaxSize = 20;

        public WaveDirector(
            IReadOnlyList<WaveData> waveDataList,
            SpawnPointManager spawnPointManager,
            Transform playerTransform,
            IDamageable playerDamageable,
            EnemyController prefab,
            Transform poolParent)
        {
            _waveDataList = waveDataList;
            _spawnPointManager = spawnPointManager;
            _playerTransform = playerTransform;
            _playerDamageable = playerDamageable;

            _activeWaveNumber = _waveDataList.Count > 0 ? _waveDataList[0].WaveNumber : 1;

            _enemyPool = new ObjectPool<EnemyController>(prefab, poolParent, PoolInitialSize, PoolMaxSize);
            EventBus.Subscribe<EnemyDiedEvent>(HandleEnemyDied);
        }

        /// <summary>
        /// AI Director が生成した RuntimeWaveConfig を次の StartWave に渡す。
        /// CurrentWaveNumber も即座に更新するため、
        /// BattleReadyState の演出表示が正しいウェーブ番号を参照できる。
        /// </summary>
        public void SetPendingWaveConfig(RuntimeWaveConfig config)
        {
            _pendingConfig = config;
            _activeWaveNumber = config.WaveNumber;
        }

        /// <summary>
        /// 現在のウェーブを開始し、敵を順次スポーンする。
        /// _pendingConfig が設定されていればそれを使用し（AI Director 経由）、
        /// なければ _waveDataList の現在インデックスから読み取る（Wave 1 用）。
        /// </summary>
        public async UniTask StartWave(CancellationToken cancellationToken)
        {
            if (_pendingConfig != null)
            {
                await StartWaveFromConfig(_pendingConfig, cancellationToken);
                _pendingConfig = null;
                return;
            }

            if (_currentWaveIndex >= _waveDataList.Count) return;

            var waveData = _waveDataList[_currentWaveIndex];
            _activeEnemyCount = waveData.TotalEnemyCount;
            _activeWaveNumber = waveData.WaveNumber;

            OnWaveStarted?.Invoke(_activeWaveNumber);
            EventBus.Publish(new WaveStartedEvent(_activeWaveNumber));

            foreach (var entry in waveData.SpawnEntries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SpawnEnemy(entry.EnemyData);

                    if (i < entry.Count - 1 && entry.SpawnDelay > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(entry.SpawnDelay),
                            cancellationToken: cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// 次ウェーブへインデックスを進める。BattleFlowEntryPoint が WaveCleared 後に呼ぶ。
        /// </summary>
        public void AdvanceToNextWave()
        {
            _currentWaveIndex++;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            EventBus.Unsubscribe<EnemyDiedEvent>(HandleEnemyDied);
            _enemyPool.Clear();
        }

        /// <summary>
        /// RuntimeWaveConfig に基づいてウェーブを実行する。
        /// AI Director が生成したエンドレスウェーブや、修正済み base wave に対応する。
        /// HealthMultiplier を SpawnEnemy に渡すことで、敵の HP スケーリングを適用する。
        /// </summary>
        private async UniTask StartWaveFromConfig(RuntimeWaveConfig config, CancellationToken cancellationToken)
        {
            _activeEnemyCount = config.TotalEnemyCount;
            _activeWaveNumber = config.WaveNumber;

            OnWaveStarted?.Invoke(_activeWaveNumber);
            EventBus.Publish(new WaveStartedEvent(_activeWaveNumber));

            foreach (var entry in config.SpawnEntries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SpawnEnemy(entry.EnemyData, config.HealthMultiplier);

                    if (i < entry.Count - 1 && entry.SpawnDelay > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(entry.SpawnDelay),
                            cancellationToken: cancellationToken);
                    }
                }
            }
        }

        private void SpawnEnemy(EnemyData enemyData, float healthMultiplier = 1f)
        {
            var enemy = _enemyPool.Get();
            enemy.SetReturnCallback(HandleEnemyReturnedToPool);

            Vector3 spawnPosition = _spawnPointManager.GetSpawnPosition(_playerTransform.position);
            enemy.transform.position = spawnPosition;
            enemy.transform.rotation = Quaternion.LookRotation(
                _playerTransform.position - spawnPosition);

            enemy.Initialize(enemyData, _playerTransform, _playerDamageable, healthMultiplier);
        }

        private void HandleEnemyDied(EnemyDiedEvent eventData)
        {
            _activeEnemyCount--;

            if (_activeEnemyCount <= 0)
            {
                EventBus.Publish(new WaveClearedEvent(_activeWaveNumber));
                OnWaveCleared?.Invoke(_activeWaveNumber);
            }
        }

        private void HandleEnemyReturnedToPool(EnemyController enemy)
        {
            _enemyPool.Return(enemy);
        }
    }
}
