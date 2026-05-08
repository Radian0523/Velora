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
    ///
    /// VContainer の IStartable / ITickable / IDisposable を実装し、
    /// コンポーネント初期化・ステート登録・イベント購読・ゲームループ駆動を行う。
    /// MonoBehaviour ではないため Update や OnDestroy を使わず、
    /// VContainer のライフサイクルに従って管理される。
    ///
    /// AI Director 統合:
    /// Wave 1 は既存の WaveData でそのまま開始する（パフォーマンスデータがないため）。
    /// Wave 2 以降は AI Director がプレイヤーのパフォーマンスに基づいて
    /// RuntimeWaveConfig を生成し、WaveDirector に渡す。
    /// Wave 4 以降はエンドレスモードとして AI Director がウェーブを自動生成し続け、
    /// ゲーム終了はプレイヤー死亡のみとなる。
    /// </summary>
    public class BattleFlowEntryPoint : IStartable, ITickable, IDisposable
    {
        private readonly PlayerModel _playerModel;
        private readonly ScoreManager _scoreManager;
        private readonly UpgradeManager _upgradeManager;
        private readonly GameFlowManager _gameFlowManager;
        private readonly BattleConfig _config;
        private readonly AIDirector _aiDirector;

        private readonly PlayerDamageReceiver _playerDamageReceiver;
        private readonly FPSController _fpsController;
        private readonly WeaponController _weaponController;
        private readonly SpawnPointManager _spawnPointManager;
        private readonly WaveEffectView _waveEffectView;

        private readonly HudPresenter _hudPresenter;
        private readonly UpgradeSelectPresenter _upgradeSelectPresenter;
        private readonly ResultPresenter _resultPresenter;
        private readonly DamageDirectionView _damageDirectionView;

        private WaveDirector _waveDirector;
        private int _currentWaveIndex;

        public BattleFlowEntryPoint(
            PlayerModel playerModel,
            ScoreManager scoreManager,
            UpgradeManager upgradeManager,
            GameFlowManager gameFlowManager,
            BattleConfig config,
            AIDirector aiDirector,
            PlayerDamageReceiver playerDamageReceiver,
            FPSController fpsController,
            WeaponController weaponController,
            SpawnPointManager spawnPointManager,
            WaveEffectView waveEffectView,
            HudPresenter hudPresenter,
            UpgradeSelectPresenter upgradeSelectPresenter,
            ResultPresenter resultPresenter,
            DamageDirectionView damageDirectionView)
        {
            _playerModel = playerModel;
            _scoreManager = scoreManager;
            _upgradeManager = upgradeManager;
            _gameFlowManager = gameFlowManager;
            _config = config;
            _aiDirector = aiDirector;
            _playerDamageReceiver = playerDamageReceiver;
            _fpsController = fpsController;
            _weaponController = weaponController;
            _spawnPointManager = spawnPointManager;
            _waveEffectView = waveEffectView;
            _hudPresenter = hudPresenter;
            _upgradeSelectPresenter = upgradeSelectPresenter;
            _resultPresenter = resultPresenter;
            _damageDirectionView = damageDirectionView;
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
            _aiDirector?.Dispose();
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

            _damageDirectionView.Initialize(_playerDamageReceiver.transform, Camera.main);
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
            }

            EventBus.Unsubscribe<PlayerDamagedEvent>(HandlePlayerDamaged);
            EventBus.Unsubscribe<WaveClearedEvent>(HandleWaveClearSound);
            EventBus.Unsubscribe<AmmoPickedUpEvent>(HandleAmmoPickup);
        }

        // --- ゲームフロー ---

        /// <summary>
        /// Wave 1 は既存の WaveData でそのまま開始する。
        /// AI Director のパフォーマンスデータがまだないため、調整なしで実行する。
        /// </summary>
        private async UniTaskVoid RunBattleFlow()
        {
            await _gameFlowManager.ChangeState(GameState.BattleReady);
            await _gameFlowManager.ChangeState(GameState.BattleInProgress);
        }

        /// <summary>
        /// エンドレスモード: ウェーブクリア後は常に次のウェーブへ進む。
        /// AI Director が前ウェーブのパフォーマンスを評価し、次ウェーブの構成を決定する。
        /// </summary>
        private void HandleWaveCleared(int waveNumber)
        {
            _currentWaveIndex++;
            RunWaveTransition().Forget();
        }

        /// <summary>
        /// ウェーブ遷移シーケンス。
        /// 1. クリア演出を再生
        /// 2. AI Director が次ウェーブの RuntimeWaveConfig を生成（前ウェーブのメトリクス使用）
        /// 3. メトリクスをリセット（次ウェーブの計測に備える）
        /// 4. WaveDirector に pending config を設定（BattleReadyState が正しい番号を参照できる）
        /// 5. アップグレード選択 → 開始演出 → 戦闘開始
        /// </summary>
        private async UniTaskVoid RunWaveTransition()
        {
            await _gameFlowManager.ChangeState(GameState.WaveCleared);

            var nextConfig = _aiDirector.BuildNextWaveConfig(_currentWaveIndex);
            _aiDirector.ResetWaveMetrics();
            _waveDirector.SetPendingWaveConfig(nextConfig);

            await _gameFlowManager.ChangeState(GameState.UpgradeSelect);
            await _gameFlowManager.ChangeState(GameState.BattleReady);
            await _gameFlowManager.ChangeState(GameState.BattleInProgress);
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
