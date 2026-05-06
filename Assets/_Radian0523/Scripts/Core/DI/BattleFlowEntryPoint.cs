using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Velora.Data;
using Velora.Player;
using Velora.UI;
using Velora.Upgrade;
using Velora.Wave;
using Velora.Weapon;

namespace Velora.Core
{
    /// <summary>
    /// Battle シーンのエントリーポイント（pure C# クラス）。
    /// BattleSceneDirector が担っていた全責務を吸収する。
    ///
    /// VContainer の IStartable / ITickable / IDisposable を実装し、
    /// コンポーネント初期化・ステート登録・イベント購読・ゲームループ駆動を行う。
    /// MonoBehaviour ではないため Update や OnDestroy を使わず、
    /// VContainer のライフサイクルに従って管理される。
    ///
    /// 設計意図:
    /// - 全依存をコンストラクタで受け取ることで、依存関係が明示的になる
    /// - WaveDirector や GameFlowManager のステート登録など、
    ///   構築後の初期化が必要な処理は Start() で実行する
    /// - Tick() で毎フレームの更新処理を駆動する
    /// </summary>
    public class BattleFlowEntryPoint : IStartable, ITickable, IDisposable
    {
        private readonly PlayerModel _playerModel;
        private readonly ScoreManager _scoreManager;
        private readonly UpgradeManager _upgradeManager;
        private readonly GameFlowManager _gameFlowManager;
        private readonly BattleConfig _config;

        private readonly PlayerDamageReceiver _playerDamageReceiver;
        private readonly FPSController _fpsController;
        private readonly WeaponController _weaponController;
        private readonly SpawnPointManager _spawnPointManager;
        private readonly WaveEffectView _waveEffectView;

        private readonly HudPresenter _hudPresenter;
        private readonly UpgradeSelectPresenter _upgradeSelectPresenter;
        private readonly ResultPresenter _resultPresenter;

        private WaveDirector _waveDirector;

        public BattleFlowEntryPoint(
            PlayerModel playerModel,
            ScoreManager scoreManager,
            UpgradeManager upgradeManager,
            GameFlowManager gameFlowManager,
            BattleConfig config,
            PlayerDamageReceiver playerDamageReceiver,
            FPSController fpsController,
            WeaponController weaponController,
            SpawnPointManager spawnPointManager,
            WaveEffectView waveEffectView,
            HudPresenter hudPresenter,
            UpgradeSelectPresenter upgradeSelectPresenter,
            ResultPresenter resultPresenter)
        {
            _playerModel = playerModel;
            _scoreManager = scoreManager;
            _upgradeManager = upgradeManager;
            _gameFlowManager = gameFlowManager;
            _config = config;
            _playerDamageReceiver = playerDamageReceiver;
            _fpsController = fpsController;
            _weaponController = weaponController;
            _spawnPointManager = spawnPointManager;
            _waveEffectView = waveEffectView;
            _hudPresenter = hudPresenter;
            _upgradeSelectPresenter = upgradeSelectPresenter;
            _resultPresenter = resultPresenter;
        }

        public void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            InitializePlayer();
            InitializeWaveDirector();
            InitializePresenters();
            InitializeGameFlowStates();
            InitializeSettings();
            SubscribeEvents();

            RunBattleFlow().Forget();
        }

        public void Tick()
        {
            _gameFlowManager.Update();
            _scoreManager.UpdateSurvivalTime(Time.deltaTime);
        }

        public void Dispose()
        {
            UnsubscribeEvents();
            _waveDirector?.Dispose();
            _scoreManager?.Dispose();
        }

        private void InitializePlayer()
        {
            _playerDamageReceiver.Initialize(_playerModel);
            _weaponController.Initialize(_playerModel);
        }

        private void InitializeWaveDirector()
        {
            // WaveDirector はプレイヤーの Transform や IDamageable など
            // ランタイム固有の参照が必要なため、DI コンテナではなくここで手動生成する。
            _waveDirector = new WaveDirector(
                _config.WaveDataList,
                _spawnPointManager,
                _playerDamageReceiver.transform,
                _playerDamageReceiver,
                _config.EnemyPrefab,
                _config.PoolParent);
        }

        private void InitializePresenters()
        {
            _hudPresenter.Initialize(_playerModel, _weaponController);
            _upgradeSelectPresenter.Initialize(_upgradeManager, _playerModel);
            _resultPresenter.Initialize(_scoreManager);
        }

        /// <summary>
        /// GameFlowManager にゲームステートを登録する。
        /// ステートクラスは View や Manager への参照を必要とするため、
        /// 全コンポーネントの初期化完了後にここで生成・登録する。
        /// </summary>
        private void InitializeGameFlowStates()
        {
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

        private void InitializeSettings()
        {
            CommonUIDirector.Instance?.PausePresenter?.SettingsPresenter
                ?.Initialize(_fpsController);
        }

        private void SubscribeEvents()
        {
            _playerModel.OnDeath += HandlePlayerDeath;
            _playerModel.OnUpgradeApplied += HandleUpgradeApplied;
            _waveDirector.OnWaveCleared += HandleWaveCleared;
            _waveDirector.OnAllWavesComplete += HandleAllWavesComplete;

            EventBus.Subscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Subscribe<WaveClearedEvent>(HandleWaveClearSound);
            EventBus.Subscribe<AmmoPickedUpEvent>(HandleAmmoPickup);
        }

        private void UnsubscribeEvents()
        {
            if (_playerModel != null)
            {
                _playerModel.OnDeath -= HandlePlayerDeath;
                _playerModel.OnUpgradeApplied -= HandleUpgradeApplied;
            }

            if (_waveDirector != null)
            {
                _waveDirector.OnWaveCleared -= HandleWaveCleared;
                _waveDirector.OnAllWavesComplete -= HandleAllWavesComplete;
            }

            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<WaveClearedEvent>(HandleWaveClearSound);
            EventBus.Unsubscribe<AmmoPickedUpEvent>(HandleAmmoPickup);
        }

        // --- ゲームフロー ---

        private async UniTaskVoid RunBattleFlow()
        {
            await _gameFlowManager.ChangeState(GameState.BattleReady);
            await _gameFlowManager.ChangeState(GameState.BattleInProgress);
        }

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

        private void HandleUpgradeApplied(UpgradeData upgrade)
        {
            if (upgrade.UpgradeType == UpgradeType.NewWeapon && upgrade.WeaponData != null)
            {
                _weaponController.AddWeapon(upgrade.WeaponData);
            }
        }

        private void HandlePlayerDeath()
        {
            PlayBattleSound(_config.BattleSoundData?.PlayerDeathSound);
            _gameFlowManager.ChangeState(GameState.GameOver).Forget();
        }

        // --- サウンドイベントハンドラ ---

        private void HandlePlayerDamaged(PlayerDamagedEvent e)
        {
            PlayBattleSound(_config.BattleSoundData?.PlayerDamageSound);
        }

        private void HandleWaveClearSound(WaveClearedEvent e)
        {
            PlayBattleSound(_config.BattleSoundData?.WaveClearSound);
        }

        private void HandleAmmoPickup(AmmoPickedUpEvent e)
        {
            PlayBattleSound(_config.BattleSoundData?.AmmoPickupSound);
        }

        private void PlayBattleSound(AudioClip clip)
        {
            AudioHelper.PlaySE(clip);
        }
    }
}
