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

            // Ranged はチャージ完了後に自前でアニメーションを発火するため、
            // AttackState ではアニメーション再生を Behavior に委ねる。
            // Rusher は Attack 前に即座に再生する。
            if (Controller.AttackBehavior is not RangedAttack)
            {
                Controller.PlayAnimation(EnemyController.AnimAttack);
            }

            await Controller.AttackBehavior.Attack(Controller);
            Controller.PlayAnimation(EnemyController.AnimIdle);
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
