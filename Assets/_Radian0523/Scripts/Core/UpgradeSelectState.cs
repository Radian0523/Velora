using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// アップグレード選択ステート（Phase 5 で本実装予定）。
    /// 現時点では即座に完了し、次の BattleReady ステートへ進む。
    /// </summary>
    public class UpgradeSelectState : GameStateBase
    {
        public override UniTask Enter() => UniTask.CompletedTask;
    }
}
