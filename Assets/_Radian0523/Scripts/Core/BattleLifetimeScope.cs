using VContainer;
using VContainer.Unity;

namespace Velora.Core
{
    /// <summary>
    /// Battle シーンの DI コンテナ設定。
    /// Phase 4 では BattleSceneDirector が手動配線するため最小限。
    /// Phase 5 以降でアップグレードシステム等の登録を追加する。
    /// </summary>
    public class BattleLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
        }
    }
}
