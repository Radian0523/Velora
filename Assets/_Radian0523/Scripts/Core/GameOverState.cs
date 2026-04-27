using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// ゲームオーバーステート（Phase 6 で本実装予定）。
    /// 現時点では即座に完了する。Result シーンへの遷移などは今後追加する。
    /// </summary>
    public class GameOverState : GameStateBase
    {
        public override UniTask Enter() => UniTask.CompletedTask;
    }
}
