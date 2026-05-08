using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
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
    /// Battle シーンの DI コンテナ設定。
    /// BattleSceneDirector が手動配線していた全依存を VContainer に移譲する。
    /// SerializeField は LifetimeScope が保持し、pure C# クラスは Register で、
    /// MonoBehaviour は RegisterComponent で登録する。
    ///
    /// WaveDirector と GameFlowManager のステート登録は構築後の初期化が必要なため、
    /// BattleFlowEntryPoint.Start() で実行する。
    /// </summary>
    public class BattleLifetimeScope : LifetimeScope
    {
        [Header("プレイヤー")]
        [SerializeField] private PlayerDamageReceiver _playerDamageReceiver;
        [SerializeField] private FPSController _fpsController;
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

        [Header("AI Director")]
        [SerializeField] private AIDirectorConfig _aiDirectorConfig;

        [Header("サウンド")]
        [SerializeField] private BattleSoundData _battleSoundData;

        [Header("UI Presenter")]
        [SerializeField] private HudPresenter _hudPresenter;
        [SerializeField] private UpgradeSelectPresenter _upgradeSelectPresenter;
        [SerializeField] private ResultPresenter _resultPresenter;
        [SerializeField] private DamageDirectionView _damageDirectionView;

        protected override void Configure(IContainerBuilder builder)
        {
            // --- Pure C# サービス ---
            // PlayerModel: プレイヤーの状態管理（maxHealth をコンストラクタ引数に渡す）
            builder.Register<PlayerModel>(Lifetime.Scoped)
                .WithParameter(_playerMaxHealth);

            builder.Register<ScoreManager>(Lifetime.Scoped);

            // UpgradeManager: IReadOnlyList<UpgradeData> をコンストラクタ引数に渡す
            // UpgradeData[] は IReadOnlyList<UpgradeData> を実装するため型互換性がある
            builder.Register<UpgradeManager>(Lifetime.Scoped)
                .WithParameter<IReadOnlyList<UpgradeData>>(_upgradeDataList);

            builder.Register<GameFlowManager>(Lifetime.Scoped);

            // AIDirector: パフォーマンス評価と難易度調整の中核。
            // AIDirectorConfig と base wave リストをパラメータで渡し、
            // PlayerModel はコンテナから自動解決する。
            builder.Register<AIDirector>(Lifetime.Scoped)
                .WithParameter<AIDirectorConfig>(_aiDirectorConfig)
                .WithParameter<IReadOnlyList<WaveData>>(_waveDataList);

            // --- Battle 設定値 ---
            // WaveDirector が必要とするプレハブ・親 Transform 等は型が重複するため、
            // BattleConfig にまとめて登録する。
            var config = new BattleConfig(
                _waveDataList, _enemyPrefab, _poolParent, _battleSoundData);
            builder.RegisterInstance(config);

            // --- シーン上の MonoBehaviour コンポーネント ---
            builder.RegisterComponent(_playerDamageReceiver);
            builder.RegisterComponent(_fpsController);
            builder.RegisterComponent(_weaponController);
            builder.RegisterComponent(_spawnPointManager);
            builder.RegisterComponent(_waveEffectView);
            builder.RegisterComponent(_hudPresenter);
            builder.RegisterComponent(_upgradeSelectPresenter);
            builder.RegisterComponent(_resultPresenter);
            builder.RegisterComponent(_damageDirectionView);

            // --- エントリーポイント ---
            // IStartable.Start() で初期化、ITickable.Tick() で毎フレーム更新、
            // IDisposable.Dispose() でクリーンアップを VContainer が自動実行する。
            builder.RegisterEntryPoint<BattleFlowEntryPoint>();
        }
    }
}
