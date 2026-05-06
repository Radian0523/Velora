using Cysharp.Threading.Tasks;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// フェード付きシーン遷移のサービス。
    /// CommonUIDirector.TransitionToScene の責務を純粋な C# クラスとして分離し、
    /// VContainer 経由で各 Presenter に注入することでシングルトン参照を排除する。
    /// Presenter が CommonUIDirector の存在を知る必要がなくなるため、
    /// 依存関係が明示的になりテスタビリティが向上する。
    /// </summary>
    public class SceneNavigator
    {
        private readonly SceneLoader _sceneLoader;
        private readonly FadeView _fadeView;

        public SceneNavigator(SceneLoader sceneLoader, FadeView fadeView)
        {
            _sceneLoader = sceneLoader;
            _fadeView = fadeView;
        }

        public async UniTask TransitionTo(string newSceneName, string currentSceneName = null)
        {
            await _sceneLoader.TransitionTo(
                newSceneName,
                () => _fadeView.FadeOut(),
                () => _fadeView.FadeIn(),
                currentSceneName);
        }
    }
}
