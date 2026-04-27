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
        private bool _isDisposed;

        public int CurrentWaveNumber => _currentWaveIndex < _waveDataList.Count
            ? _waveDataList[_currentWaveIndex].WaveNumber
            : -1;

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

            _enemyPool = new ObjectPool<EnemyController>(prefab, poolParent, PoolInitialSize, PoolMaxSize);
            EventBus.Subscribe<EnemyDiedEvent>(HandleEnemyDied);
        }

        /// <summary>
        /// 現在のウェーブを開始し、SpawnEntry の定義に従って敵を順次スポーンする。
        /// SpawnEntry 間のディレイで緩急をつけ、プレイヤーが対処しやすくする。
        /// </summary>
        public async UniTask StartWave(CancellationToken cancellationToken)
        {
            if (_currentWaveIndex >= _waveDataList.Count) return;

            var waveData = _waveDataList[_currentWaveIndex];
            _activeEnemyCount = waveData.TotalEnemyCount;

            int waveNumber = waveData.WaveNumber;
            OnWaveStarted?.Invoke(waveNumber);
            EventBus.Publish(new WaveStartedEvent(waveNumber));

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
        /// 次ウェーブへインデックスを進める。BattleSceneDirector が WaveCleared 後に呼ぶ。
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

        private void SpawnEnemy(EnemyData enemyData)
        {
            var enemy = _enemyPool.Get();
            enemy.SetReturnCallback(HandleEnemyReturnedToPool);

            Vector3 spawnPosition = _spawnPointManager.GetSpawnPosition(_playerTransform.position);
            enemy.transform.position = spawnPosition;
            enemy.transform.rotation = Quaternion.LookRotation(
                _playerTransform.position - spawnPosition);

            enemy.Initialize(enemyData, _playerTransform, _playerDamageable);
        }

        private void HandleEnemyDied(EnemyDiedEvent eventData)
        {
            _activeEnemyCount--;

            if (_activeEnemyCount <= 0)
            {
                int waveNumber = _waveDataList[_currentWaveIndex].WaveNumber;
                EventBus.Publish(new WaveClearedEvent(waveNumber));
                OnWaveCleared?.Invoke(waveNumber);

                if (!HasNextWave)
                {
                    OnAllWavesComplete?.Invoke();
                }
            }
        }

        private void HandleEnemyReturnedToPool(EnemyController enemy)
        {
            _enemyPool.Return(enemy);
        }
    }
}
