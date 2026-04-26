using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 攻撃ステート。IAttackBehavior に攻撃処理を委譲する。
    /// クールダウン経過かつ射程内なら再攻撃、射程外なら Chase へ遷移する。
    /// </summary>
    public class AttackState : EnemyStateBase
    {
        private float _lastAttackTime;
        private bool _isAttacking;

        public override UniTask Enter()
        {
            Controller.Agent.isStopped = true;
            _isAttacking = false;

            // 初回攻撃を即座に実行するため、クールダウン経過済みとして初期化
            _lastAttackTime = -Controller.Data.AttackCooldown;

            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            if (_isAttacking) return;

            LookAtPlayer();

            float distance = Vector3.Distance(
                Controller.transform.position,
                Controller.PlayerTransform.position);

            if (distance > Controller.Data.AttackRange)
            {
                StateMachine.ChangeState(EnemyState.Chase).Forget();
                return;
            }

            if (Time.time - _lastAttackTime >= Controller.Data.AttackCooldown)
            {
                ExecuteAttack().Forget();
            }
        }

        private async UniTaskVoid ExecuteAttack()
        {
            _isAttacking = true;
            await Controller.AttackBehavior.Attack(Controller);
            _isAttacking = false;
            _lastAttackTime = Time.time;
        }

        private void LookAtPlayer()
        {
            var direction = Controller.PlayerTransform.position - Controller.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Controller.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
