namespace Velora.Core
{
    public enum GameState
    {
        Title,
        BattleReady,
        BattleInProgress,
        WaveCleared,
        UpgradeSelect,
        GameOver,
        Result
    }

    /// <summary>
    /// ゲーム全体のフローをステートマシンで管理する pure C# クラス。
    /// 各ステートが Enter/Update/Exit で自分の責務を完結させることで、
    /// 新しいステートの追加がクラス1つの作成で完結する。
    /// 遷移ロジックは汎用 StateMachine に委譲し、ステート登録時の
    /// オーナー設定のみをオーバーライドで差し込む。
    /// </summary>
    public class GameFlowManager : StateMachine<GameState, GameStateBase>
    {
        protected override void InitializeState(GameStateBase stateInstance)
        {
            stateInstance.SetOwner(this);
        }
    }
}
