using Cysharp.Threading.Tasks;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵ステートの基底クラス。
    /// GameStateBase と同一パターンで、Enter/Update/Exit のライフサイクルを提供する。
    /// 各ステートはこのクラスを継承し、自身の責務を完結させる。
    /// </summary>
    public abstract class EnemyStateBase
    {
        protected EnemyStateMachine StateMachine { get; private set; }
        protected EnemyController Controller => StateMachine.Controller;

        public void SetStateMachine(EnemyStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual UniTask Enter() => UniTask.CompletedTask;
        public virtual void Update() { }
        public virtual UniTask Exit() => UniTask.CompletedTask;
    }
}
