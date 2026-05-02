using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// スポーン演出待機ステート。
    /// タイマー経過後に Idle へ遷移する。演出は Prefab 未設定でもスキップされる。
    /// </summary>
    public class SpawnState : EnemyStateBase
    {
        private const float SpawnAnimationDuration = 0.5f;

        private float _timer;

        public override UniTask Enter()
        {
            _timer = 0f;
            Controller.Agent.isStopped = true;
            Controller.PlayAnimation(EnemyController.AnimIdle);

            if (Controller.Data.SpawnEffectPrefab != null)
            {
                Object.Instantiate(
                    Controller.Data.SpawnEffectPrefab,
                    Controller.transform.position,
                    Quaternion.identity);
            }

            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= SpawnAnimationDuration)
            {
                StateMachine.ChangeState(EnemyState.Idle).Forget();
            }
        }
    }
}
