using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// ステートマシンのステートインターフェース。
    /// GameStateBase と EnemyStateBase の共通契約を定義し、
    /// 汎用 StateMachine から型安全に呼び出せるようにする。
    /// </summary>
    public interface IState
    {
        UniTask Enter();
        void Update();
        UniTask Exit();
    }
}
