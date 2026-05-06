using VContainer;
using VContainer.Unity;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// Title シーンの DI コンテナ設定。
    /// RootLifetimeScope（親スコープ）から SceneNavigator を解決し、
    /// TitlePresenter に注入する。
    /// RegisterComponentInHierarchy で Title シーン内の TitlePresenter を
    /// 自動検出するため、SerializeField による手動割当が不要。
    /// </summary>
    public class TitleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<TitlePresenter>();
        }
    }
}
