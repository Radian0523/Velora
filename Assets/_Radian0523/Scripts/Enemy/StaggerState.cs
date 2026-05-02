using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 怯みステート。EnemyData.StaggerDuration の間動作を停止し、
    /// 経過後に Chase へ復帰する。
    /// </summary>
    public class StaggerState : EnemyStateBase
    {
        private float _timer;

        public override UniTask Enter()
        {
            _timer = 0f;
            Controller.Agent.isStopped = true;
            Controller.PlayAnimation(EnemyController.AnimGetHit);
            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= Controller.Data.StaggerDuration)
            {
                StateMachine.ChangeState(EnemyState.Chase).Forget();
            }
        }
    }
}
