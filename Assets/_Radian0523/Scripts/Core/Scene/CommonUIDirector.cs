using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Velora.Data;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// CommonUI シーン（Additive 常駐）のサービスロケーター。
    /// FadeView・SceneLoader など、シーンをまたいで利用するサービスへの
    /// アクセスを一元管理する。
    ///
    /// VContainer 移行後は RootLifetimeScope が各サービスを生成し、
    /// [Inject] で受け取る互換シムとして機能する。
    /// Instance プロパティは既存の参照箇所（Title/Result シーン等）の
    /// 段階的移行のために維持する。
    /// </summary>
    public class CommonUIDirector : MonoBehaviour
    {
        public static CommonUIDirector Instance { get; private set; }

        [SerializeField] private FadeView _fadeView;
        [SerializeField] private PausePresenter _pausePresenter;

        public FadeView FadeView => _fadeView;
        public SceneLoader SceneLoader { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public FontThemeService FontThemeService { get; private set; }
        public UISoundData UISoundData { get; private set; }
        public PausePresenter PausePresenter => _pausePresenter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// RootLifetimeScope が生成したサービスを受け取る。
        /// Awake で手動 new していた生成処理を DI に移譲することで、
        /// サービスのライフサイクル管理が VContainer に統一される。
        /// </summary>
        [Inject]
        public void Construct(
            AudioManager audioManager,
            SceneLoader sceneLoader,
            FontThemeService fontThemeService,
            UISoundData uiSoundData)
        {
            AudioManager = audioManager;
            SceneLoader = sceneLoader;
            FontThemeService = fontThemeService;
            UISoundData = uiSoundData;
        }

        private void OnDestroy()
        {
            AudioManager?.Dispose();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// フェード付きシーン遷移のヘルパー。
        /// SceneLoader.TransitionTo に FadeView のデリゲートを渡すことで、
        /// 呼び出し側がフェード処理の詳細を知らなくてよい。
        /// </summary>
        public async UniTask TransitionToScene(string newSceneName, string currentSceneName = null)
        {
            await SceneLoader.TransitionTo(
                newSceneName,
                () => _fadeView.FadeOut(),
                () => _fadeView.FadeIn(),
                currentSceneName);
        }
    }
}
