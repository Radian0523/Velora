using UnityEngine;
using Cysharp.Threading.Tasks;
using Velora.Data;
using Velora.Enemy;
using Velora.Player;
using Velora.UI;
using Velora.Upgrade;
using Velora.Wave;
using Velora.Weapon;

namespace Velora.Core
{
    /// <summary>
    /// Battle シーンのオーケストレーター。
    /// PlayerModel・WaveDirector・GameFlowManager・各 Presenter を生成・接続し、
    /// 「BattleReady → BattleInProgress → WaveCleared → UpgradeSelect → 次Wave」
    /// のゲームループを駆動する。各システム間の依存を一箇所で管理し、
    /// 個々のクラスの疎結合を維持する。
    /// </summary>
    public class BattleSceneDirector : MonoBehaviour
    {
        [Header("プレイヤー")]
        [SerializeField] private PlayerDamageReceiver _playerDamageReceiver;
        [SerializeField] private float _playerMaxHealth = 100f;

        [Header("武器")]
        [SerializeField] private WeaponController _weaponController;

        [Header("ウェーブ")]
        [SerializeField] private EnemyController _enemyPrefab;
        [SerializeField] private WaveData[] _waveDataList;
        [SerializeField] private SpawnPointManager _spawnPointManager;
        [SerializeField] private Transform _poolParent;

        [Header("アップグレード")]
        [SerializeField] private UpgradeData[] _upgradeDataList;

        [Header("UI")]
        [SerializeField] private WaveEffectView _waveEffectView;

        [Header("UI Presenter")]
        [SerializeField] private HudPresenter _hudPresenter;
        [SerializeField] private UpgradeSelectPresenter _upgradeSelectPresenter;
        [SerializeField] private ResultPresenter _resultPresenter;

        private PlayerModel _playerModel;
        private WaveDirector _waveDirector;
        private GameFlowManager _gameFlowManager;
        private ScoreManager _scoreManager;
        private UpgradeManager _upgradeManager;

        private void Start()
        {
            // Battle シーンではカーソルをロック（Title シーンやリザルトでは解除されている）
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializePlayer();
            InitializeScore();
            InitializeWaveDirector();
            InitializeGameFlowManager();
            SubscribeEvents();

            // CommonUIDirector が存在する場合はフェードインで開始演出を行う。
            // エディタで Battle シーンを直接再生した場合は CommonUI がないためスキップ。
            if (CommonUIDirector.Instance != null)
            {
                CommonUIDirector.Instance.FadeView.FadeIn().Forget();
            }

            RunBattleFlow().Forget();
        }

        private void Update()
        {
            _gameFlowManager?.Update();
            _scoreManager?.UpdateSurvivalTime(Time.deltaTime);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            _waveDirector?.Dispose();
            _scoreManager?.Dispose();
        }

        private void InitializePlayer()
        {
            _playerModel = new PlayerModel(_playerMaxHealth);
            _playerDamageReceiver.Initialize(_playerModel);
        }

        private void InitializeScore()
        {
            _scoreManager = new ScoreManager();
            _upgradeManager = new UpgradeManager(_upgradeDataList);

            // Presenter の配線は全システム初期化後に行う。
            // _playerModel は InitializePlayer() で生成済みであることが前提。
            _hudPresenter.Initialize(_playerModel, _weaponController);
            _upgradeSelectPresenter.Initialize(_upgradeManager, _playerModel);
            _resultPresenter.Initialize(_scoreManager);
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
                new UpgradeSelectState(_upgradeSelectPresenter));

            _gameFlowManager.RegisterState(
                GameState.GameOver,
                new GameOverState(_resultPresenter));

            _gameFlowManager.RegisterState(
                GameState.Result,
                new ResultState(_resultPresenter));
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
            await _gameFlowManager.ChangeState(GameState.Result);
        }

        private void HandlePlayerDeath()
        {
            _gameFlowManager.ChangeState(GameState.GameOver).Forget();
        }
    }
}
