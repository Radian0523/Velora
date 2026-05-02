using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 待機ステート。巡回地点に到着後、短時間立ち止まってから次の Patrol へ遷移する。
    /// DetectionRange 内にプレイヤーを検知したら即座に Chase へ遷移する。
    /// </summary>
    public class IdleState : EnemyStateBase
    {
        private const float IdleDuration = 2f;

        private float _timer;

        public override UniTask Enter()
        {
            _timer = 0f;
            Controller.Agent.isStopped = true;
            Controller.PlayAnimation(EnemyController.AnimIdle);
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
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= IdleDuration)
            {
                StateMachine.ChangeState(EnemyState.Patrol).Forget();
            }
        }
    }
}
