using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;
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
        private readonly IObjectResolver _resolver;

        private int _activeEnemyCount;
        private int _activeWaveNumber;
        private RuntimeWaveConfig _pendingConfig;
        private bool _isDisposed;

        /// <summary>
        /// 現在のウェーブ番号。BattleReadyState / WaveClearedState が演出表示に使用する。
        /// SetPendingWaveConfig で事前に更新されるため、BattleReady 遷移時には正しい値が入る。
        /// </summary>
        public int CurrentWaveNumber => _activeWaveNumber;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCleared;

        private const int PoolInitialSize = 10;
        private const int PoolMaxSize = 20;

        public WaveDirector(
            IReadOnlyList<WaveData> waveDataList,
            SpawnPointManager spawnPointManager,
            Transform playerTransform,
            IDamageable playerDamageable,
            EnemyController prefab,
            Transform poolParent,
            IObjectResolver resolver)
        {
            _waveDataList = waveDataList;
            _spawnPointManager = spawnPointManager;
            _playerTransform = playerTransform;
            _playerDamageable = playerDamageable;
            _resolver = resolver;

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
        /// _pendingConfig が設定されていればそれを使用し（AI Director 経由、Wave 2 以降）、
        /// なければ先頭の WaveData をベースに RuntimeWaveConfig へ正規化する（Wave 1 用）。
        /// Wave 2 以降は AI Director が SetPendingWaveConfig で構成を差し込むため、
        /// 固定 WaveData リストは Wave 1 の初期構成としてのみ参照する。
        /// いずれの経路も ExecuteWave に集約し、スポーン処理を一本化する。
        /// </summary>
        public async UniTask StartWave(CancellationToken cancellationToken)
        {
            RuntimeWaveConfig config = _pendingConfig;
            if (config == null)
            {
                if (_waveDataList.Count == 0) return;
                config = RuntimeWaveConfig.FromWaveData(_waveDataList[0]);
            }

            _pendingConfig = null;
            await ExecuteWave(config, cancellationToken);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            EventBus.Unsubscribe<EnemyDiedEvent>(HandleEnemyDied);
            _enemyPool.Clear();
        }

        /// <summary>
        /// RuntimeWaveConfig に基づいてウェーブを実行する唯一のスポーン経路。
        /// Wave 1 の WaveData も、AI Director 生成のエンドレス/修正済み base wave も
        /// すべて RuntimeWaveConfig に正規化してからここを通る。
        /// HealthMultiplier を SpawnEnemy に渡すことで、敵の HP スケーリングを適用する。
        /// </summary>
        private async UniTask ExecuteWave(RuntimeWaveConfig config, CancellationToken cancellationToken)
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

            // プールから取得した敵に VContainer の依存を注入する。
            // EnemyController の [Inject] Construct が呼ばれ、AudioManager 等が渡される。
            _resolver.InjectGameObject(enemy.gameObject);

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
