using UnityEngine;
using VContainer;
using VContainer.Unity;
using Velora.Data;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// アプリケーション全体の DI コンテナ設定。
    /// CommonUI シーン（Additive 常駐）のサービスを登録する。
    /// 子スコープ（BattleLifetimeScope 等）から親スコープのサービスを解決可能にすることで、
    /// AudioManager や SceneLoader をシーン横断で共有する。
    /// </summary>
    public class RootLifetimeScope : LifetimeScope
    {
        [Header("Audio")]
        [SerializeField] private AudioManagerHost _audioManagerHost;

        [Header("Font")]
        [SerializeField] private FontThemeData[] _fontThemes;

        [Header("UI")]
        [SerializeField] private CommonUIDirector _commonUIDirector;
        [SerializeField] private FadeView _fadeView;
        [SerializeField] private PausePresenter _pausePresenter;
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [SerializeField] private LanguageSwitchPresenter _languageSwitchPresenter;

        [Header("UI Sound")]
        [SerializeField] private UISoundData _uiSoundData;

        protected override void Configure(IContainerBuilder builder)
        {
            // シーン横断サービス: AudioManager は AudioManagerHost(MonoBehaviour)を引数に取る
            builder.Register<AudioManager>(Lifetime.Singleton)
                .WithParameter(_audioManagerHost);

            builder.Register<SceneLoader>(Lifetime.Singleton);

            builder.Register<FontThemeService>(Lifetime.Singleton)
                .WithParameter(_fontThemes);

            // フェード付きシーン遷移を SceneNavigator に集約する。
            // Presenter は CommonUIDirector.Instance ではなく SceneNavigator を
            // 注入してもらうことで、シングルトン参照を排除できる。
            builder.Register<SceneNavigator>(Lifetime.Singleton);

            // MonoBehaviour コンポーネント: シーン上の既存インスタンスを登録
            // CommonUIDirector を登録することで [Inject] Construct() が呼ばれ、
            // AudioManager / SceneLoader / FontThemeService が注入される。
            builder.RegisterComponent(_commonUIDirector);
            builder.RegisterComponent(_fadeView);
            builder.RegisterComponent(_pausePresenter);
            builder.RegisterComponent(_settingsPresenter);
            builder.RegisterComponent(_languageSwitchPresenter);

            // ScriptableObject データ: 読み取り専用のためインスタンス登録
            builder.RegisterInstance(_uiSoundData);
        }
    }
}
