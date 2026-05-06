using Velora.Core;

namespace Velora.Enemy
{
    public enum EnemyState
    {
        Spawn,
        Idle,
        Patrol,
        Chase,
        Attack,
        Stagger,
        Death
    }

    /// <summary>
    /// 敵AI のステートマシン。
    /// 汎用 StateMachine の遷移ロジックを継承し、
    /// ステート登録時の EnemyController 参照設定のみをオーバーライドで差し込む。
    /// </summary>
    public class EnemyStateMachine : StateMachine<EnemyState, EnemyStateBase>
    {
        public EnemyController Controller { get; }

        public EnemyStateMachine(EnemyController controller)
        {
            Controller = controller;
        }

        protected override void InitializeState(EnemyStateBase stateInstance)
        {
            stateInstance.SetStateMachine(this);
        }
    }
}
