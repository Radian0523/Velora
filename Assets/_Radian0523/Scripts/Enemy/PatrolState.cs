using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Velora.Enemy
{
    /// <summary>
    /// 巡回ステート。スポーン地点周辺のランダムな NavMesh 上の地点へ歩く。
    /// プレイヤー検知で Chase へ、目的地到着で Idle へ遷移する。
    /// Idle との「歩く → 立ち止まる → 歩く」ループで自然な巡回行動を実現する。
    /// </summary>
    public class PatrolState : EnemyStateBase
    {
        private const float PatrolRadius = 8f;
        private const float ArrivalThreshold = 0.5f;

        public override UniTask Enter()
        {
            Controller.Agent.isStopped = false;
            Controller.PlayAnimation(EnemyController.AnimRun);

            Vector3 destination = PickRandomDestination();
            Controller.Agent.SetDestination(destination);

            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            float distanceToPlayer = Vector3.Distance(
                Controller.transform.position,
                Controller.PlayerTransform.position);

            if (distanceToPlayer <= Controller.Data.DetectionRange)
            {
                StateMachine.ChangeState(EnemyState.Chase).Forget();
                return;
            }

            if (HasArrivedAtDestination())
            {
                StateMachine.ChangeState(EnemyState.Idle).Forget();
            }
        }

        /// <summary>
        /// スポーン地点を中心にランダムな NavMesh 上の地点を選ぶ。
        /// NavMesh.SamplePosition で有効な地点が見つからない場合はスポーン地点に戻る。
        /// </summary>
        private Vector3 PickRandomDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * PatrolRadius;
            randomDirection += Controller.SpawnPosition;
            randomDirection.y = Controller.SpawnPosition.y;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, PatrolRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return Controller.SpawnPosition;
        }

        private bool HasArrivedAtDestination()
        {
            var agent = Controller.Agent;
            return !agent.pathPending
                && agent.remainingDistance <= ArrivalThreshold;
        }
    }
}
