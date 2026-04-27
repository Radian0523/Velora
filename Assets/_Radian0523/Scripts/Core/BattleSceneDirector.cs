using UnityEngine;
using Cysharp.Threading.Tasks;
using Velora.Data;
using Velora.Enemy;
using Velora.Player;
using Velora.Wave;

namespace Velora.Core
{
    /// <summary>
    /// Battle シーンのオーケストレーター。
    /// PlayerModel・WaveDirector・GameFlowManager を生成・接続し、
    /// 「BattleReady → BattleInProgress → WaveCleared → UpgradeSelect → 次Wave」
    /// のゲームループを駆動する。各システム間の依存を一箇所で管理し、
    /// 個々のクラスの疎結合を維持する。
    /// </summary>
    public class BattleSceneDirector : MonoBehaviour
    {
        [Header("プレイヤー")]
        [SerializeField] private PlayerDamageReceiver _playerDamageReceiver;
        [SerializeField] private float _playerMaxHealth = 100f;

        [Header("ウェーブ")]
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private WaveData[] _waveDataList;
        [SerializeField] private SpawnPointManager _spawnPointManager;
        [SerializeField] private Transform _poolParent;

        [Header("UI")]
        [SerializeField] private WaveEffectView _waveEffectView;

        private PlayerModel _playerModel;
        private WaveDirector _waveDirector;
        private GameFlowManager _gameFlowManager;

        private void Start()
        {
            InitializePlayer();
            InitializeWaveDirector();
            InitializeGameFlowManager();
            SubscribeEvents();

            RunBattleFlow().Forget();
        }

        private void Update()
        {
            _gameFlowManager?.Update();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            _waveDirector?.Dispose();
        }

        private void InitializePlayer()
        {
            _playerModel = new PlayerModel(_playerMaxHealth);
            _playerDamageReceiver.Initialize(_playerModel);
        }

        private void InitializeWaveDirector()
        {
            _waveDirector = new WaveDirector(
                _waveDataList,
                _spawnPointManager,
                _playerDamageReceiver.transform,
                _playerDamageReceiver,
                _enemyPrefab,
                _poolParent);
        }

        private void InitializeGameFlowManager()
        {
            _gameFlowManager = new GameFlowManager();

            _gameFlowManager.RegisterState(
                GameState.BattleReady,
                new BattleReadyState(_waveEffectView, _waveDirector));

            _gameFlowManager.RegisterState(
                GameState.BattleInProgress,
                new BattleInProgressState(_waveDirector));

            _gameFlowManager.RegisterState(
                GameState.WaveCleared,
                new WaveClearedState(_waveEffectView, _waveDirector));

            _gameFlowManager.RegisterState(
                GameState.UpgradeSelect,
                new UpgradeSelectState());

            _gameFlowManager.RegisterState(
                GameState.GameOver,
                new GameOverState());

            _gameFlowManager.RegisterState(
                GameState.Result,
                new ResultState());
        }

        private void SubscribeEvents()
        {
            _playerModel.OnDeath += HandlePlayerDeath;
            _waveDirector.OnWaveCleared += HandleWaveCleared;
            _waveDirector.OnAllWavesComplete += HandleAllWavesComplete;
        }

        private void UnsubscribeEvents()
        {
            if (_playerModel != null)
            {
                _playerModel.OnDeath -= HandlePlayerDeath;
            }

            if (_waveDirector != null)
            {
                _waveDirector.OnWaveCleared -= HandleWaveCleared;
                _waveDirector.OnAllWavesComplete -= HandleAllWavesComplete;
            }
        }

        /// <summary>
        /// 最初のウェーブの開始演出→戦闘開始までを駆動する。
        /// 以降のウェーブ進行は HandleWaveCleared から駆動される。
        /// </summary>
        private async UniTaskVoid RunBattleFlow()
        {
            await _gameFlowManager.ChangeState(GameState.BattleReady);
            await _gameFlowManager.ChangeState(GameState.BattleInProgress);
        }

        /// <summary>
        /// ウェーブクリア時のフロー: クリア演出 → アップグレード選択 → 次ウェーブ準備 → 戦闘開始。
        /// OnAllWavesComplete が先に発火した場合はこのフローは実行されない。
        /// </summary>
        private void HandleWaveCleared(int waveNumber)
        {
            if (!_waveDirector.HasNextWave) return;

            RunWaveTransition().Forget();
        }

        private async UniTaskVoid RunWaveTransition()
        {
            await _gameFlowManager.ChangeState(GameState.WaveCleared);
            _waveDirector.AdvanceToNextWave();
            await _gameFlowManager.ChangeState(GameState.UpgradeSelect);
            await _gameFlowManager.ChangeState(GameState.BattleReady);
            await _gameFlowManager.ChangeState(GameState.BattleInProgress);
        }

        private void HandleAllWavesComplete()
        {
            RunAllWavesClearSequence().Forget();
        }

        private async UniTaskVoid RunAllWavesClearSequence()
        {
            await _gameFlowManager.ChangeState(GameState.WaveCleared);
            // Phase 6 で Result シーンへの遷移を実装
            await _gameFlowManager.ChangeState(GameState.Result);
        }

        private void HandlePlayerDeath()
        {
            _gameFlowManager.ChangeState(GameState.GameOver).Forget();
        }
    }
}
