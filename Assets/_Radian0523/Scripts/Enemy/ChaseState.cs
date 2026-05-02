using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Data;

namespace Velora.Enemy
{
    /// <summary>
    /// 追跡ステート。NavMeshAgent でプレイヤーを追う。
    /// Ranged タイプは近すぎるとプレイヤーから後退し、適切な射撃距離を維持する。
    /// AttackRange 内に入ったら Attack へ遷移する。
    /// </summary>
    public class ChaseState : EnemyStateBase
    {
        public override UniTask Enter()
        {
            Controller.Agent.isStopped = false;
            Controller.PlayAnimation(EnemyController.AnimRun);
            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            var playerPosition = Controller.PlayerTransform.position;
            float distance = Vector3.Distance(Controller.transform.position, playerPosition);

            if (distance <= Controller.Data.AttackRange)
            {
                StateMachine.ChangeState(EnemyState.Attack).Forget();
                return;
            }

            // Ranged タイプは近すぎるとプレイヤーから後退する。
            // 2タイプのための条件分岐で十分なため、IMovementBehavior は導入しない。
            // Exploder/Tank 追加時に移動パターンが大きく異なれば、そのときリファクタする。
            if (Controller.Data.BehaviorType == EnemyBehaviorType.Ranged
                && distance < Controller.Data.MinRetreatRange)
            {
                var retreatDirection = (Controller.transform.position - playerPosition).normalized;
                var retreatTarget = Controller.transform.position
                    + retreatDirection * Controller.Data.PreferredRange;
                Controller.Agent.SetDestination(retreatTarget);
            }
            else
            {
                Controller.Agent.SetDestination(playerPosition);
            }
        }

        public override UniTask Exit()
        {
            Controller.Agent.isStopped = true;
            return UniTask.CompletedTask;
        }
    }
}
