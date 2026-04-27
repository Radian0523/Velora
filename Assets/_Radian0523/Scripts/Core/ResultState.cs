using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// リザルト表示ステート（Phase 6 で本実装予定）。
    /// 現時点では即座に完了する。スコア表示やリトライ導線は今後追加する。
    /// </summary>
    public class ResultState : GameStateBase
    {
        public override UniTask Enter() => UniTask.CompletedTask;
    }
}
