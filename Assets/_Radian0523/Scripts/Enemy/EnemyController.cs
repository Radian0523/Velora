using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using Velora.Battle;
using Velora.Core;
using Velora.Data;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵の MonoBehaviour。EnemyModel（ロジック）と EnemyStateMachine（AI）を統合し、
    /// IDamageable を実装して武器からのダメージを受け付ける。
    /// Initialize で EnemyData を受け取り、BehaviorType に応じて攻撃方式を自動選択する。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        public EnemyData Data { get; private set; }
        public EnemyModel Model { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public IDamageable PlayerDamageable { get; private set; }
        public IAttackBehavior AttackBehavior { get; private set; }

        private EnemyStateMachine _stateMachine;
        private Collider _collider;

        /// <summary>
        /// 敵を初期化する。WaveDirector やスポナーから呼び出す想定。
        /// EnemyData のパラメータから Model を構築し、BehaviorType で攻撃方式を自動選択する。
        /// </summary>
        public void Initialize(EnemyData data, Transform playerTransform, IDamageable playerDamageable)
        {
            Data = data;
            PlayerTransform = playerTransform;
            PlayerDamageable = playerDamageable;

            Agent = GetComponent<NavMeshAgent>();
            Agent.speed = data.MoveSpeed;
            _collider = GetComponent<Collider>();

            Model = new EnemyModel(data.MaxHealth, data.StaggerThreshold);
            AttackBehavior = SelectBehavior(data.BehaviorType);

            _stateMachine = new EnemyStateMachine(this);
            RegisterStates();
            SubscribeModelEvents();

            _stateMachine.ChangeState(EnemyState.Spawn).Forget();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void OnDestroy()
        {
            UnsubscribeModelEvents();
        }

        // --- IDamageable ---

        public void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot)
        {
            if (Model == null || Model.IsDead) return;

            Model.TakeDamage(damage);
            EventBus.Publish(new EnemyDamagedEvent(damage, hitPoint, isHeadshot));
        }

        // --- 外部 API ---

        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

        /// <summary>
        /// プール返却処理。ObjectPool 連携は WaveDirector 実装時に接続する。
        /// </summary>
        public void ReturnToPool()
        {
            gameObject.SetActive(false);
        }

        // --- 内部処理 ---

        /// <summary>
        /// BehaviorType に応じて攻撃方式を自動選択する（ストラテジーパターン）。
        /// WeaponController の IFireStrategy 選択と同じ設計思想。
        /// 新しい敵タイプの追加時はここに分岐を追加する。
        /// </summary>
        private IAttackBehavior SelectBehavior(EnemyBehaviorType behaviorType)
        {
            return behaviorType switch
            {
                EnemyBehaviorType.Rusher => new RusherAttack(),
                EnemyBehaviorType.Ranged => new RangedAttack(),
                _ => new RusherAttack()
            };
        }

        private void RegisterStates()
        {
            _stateMachine.RegisterState(EnemyState.Spawn, new SpawnState());
            _stateMachine.RegisterState(EnemyState.Idle, new IdleState());
            _stateMachine.RegisterState(EnemyState.Chase, new ChaseState());
            _stateMachine.RegisterState(EnemyState.Attack, new AttackState());
            _stateMachine.RegisterState(EnemyState.Stagger, new StaggerState());
            _stateMachine.RegisterState(EnemyState.Death, new DeathState());
        }

        private void SubscribeModelEvents()
        {
            Model.OnDeath += HandleDeath;
            Model.OnStaggerTriggered += HandleStagger;
        }

        private void UnsubscribeModelEvents()
        {
            if (Model == null) return;
            Model.OnDeath -= HandleDeath;
            Model.OnStaggerTriggered -= HandleStagger;
        }

        private void HandleDeath()
        {
            EventBus.Publish(new EnemyDiedEvent(Data.ScoreValue, transform.position));
            _stateMachine.ChangeState(EnemyState.Death).Forget();
        }

        private void HandleStagger()
        {
            _stateMachine.ChangeState(EnemyState.Stagger).Forget();
        }
    }
}
