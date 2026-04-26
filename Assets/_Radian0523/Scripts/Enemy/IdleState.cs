using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 待機ステート。DetectionRange 内にプレイヤーを検知したら Chase へ遷移する。
    /// </summary>
    public class IdleState : EnemyStateBase
    {
        public override UniTask Enter()
        {
            Controller.Agent.isStopped = true;
            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            float distance = Vector3.Distance(
                Controller.transform.position,
                Controller.PlayerTransform.position);

            if (distance <= Controller.Data.DetectionRange)
            {
                StateMachine.ChangeState(EnemyState.Chase).Forget();
            }
        }
    }
}
