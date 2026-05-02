using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// CommonUI シーン（Additive 常駐）のサービスロケーター。
    /// FadeView・SceneLoader など、シーンをまたいで利用するサービスへの
    /// アクセスを一元管理する。
    ///
    /// シングルトンを採用した理由:
    /// CommonUI はアプリ起動時にロードされゲーム終了まで存続するため、
    /// インスタンスの寿命がアプリケーション全体と一致する。
    /// シーン設計でインスタンスが1つであることが保証されるため、
    /// シングルトンのデメリット（寿命管理の曖昧さ）が発生しない。
    /// </summary>
    public class CommonUIDirector : MonoBehaviour
    {
        public static CommonUIDirector Instance { get; private set; }

        [SerializeField] private FadeView _fadeView;
        [SerializeField] private AudioManagerHost _audioManagerHost;
        [SerializeField] private PausePresenter _pausePresenter;

        public FadeView FadeView => _fadeView;
        public SceneLoader SceneLoader { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public PausePresenter PausePresenter => _pausePresenter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SceneLoader = new SceneLoader();
            AudioManager = new AudioManager(_audioManagerHost);
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
        public async UniTask TransitionToScene(string newSceneAddress, string currentSceneAddress = null)
        {
            await SceneLoader.TransitionTo(
                newSceneAddress,
                () => _fadeView.FadeOut(),
                () => _fadeView.FadeIn(),
                currentSceneAddress);
        }
    }
}
