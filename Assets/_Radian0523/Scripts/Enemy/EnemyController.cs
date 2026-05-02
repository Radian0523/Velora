using System;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using Velora.Battle;
using Velora.Core;
using Velora.Data;
using Velora.UI;

namespace Velora.Enemy
{
    /// <summary>
    /// 敵の MonoBehaviour。EnemyModel（ロジック）と EnemyStateMachine（AI）を統合し、
    /// IDamageable を実装して武器からのダメージを受け付ける。
    /// Initialize で EnemyData を受け取り、BehaviorType に応じて攻撃方式を自動選択する。
    /// ObjectPool 再利用に対応し、ReturnToPool でコールバック経由でプールに返却する。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        // CrossFadeInFixedTime で直接ステート名を指定するため、
        // AnimatorController 側にトランジションを組む必要がなく、
        // ステートマシンのコードが全遷移を一元管理できる。
        private const float DefaultTransitionDuration = 0.15f;
        private const float LookAtSpeed = 5f;
        private const float MaxHeadAngle = 70f;

        public static readonly int AnimIdle = Animator.StringToHash("CombatIdle");
        public static readonly int AnimRun = Animator.StringToHash("Run");
        public static readonly int AnimAttack = Animator.StringToHash("BasicAttack");
        public static readonly int AnimGetHit = Animator.StringToHash("GetHit");
        public static readonly int AnimDeath = Animator.StringToHash("Death");

        public EnemyData Data { get; private set; }
        public EnemyModel Model { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Animator Animator { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public IDamageable PlayerDamageable { get; private set; }
        public IAttackBehavior AttackBehavior { get; private set; }
        public Vector3 SpawnPosition { get; private set; }

        [SerializeField] private Transform _headBone;

        private EnemyStateMachine _stateMachine;
        private EnemyHPBarView _hpBarView;
        private Collider _collider;
        private Action<EnemyController> _returnCallback;
        private float _lookAtWeight;

        /// <summary>
        /// プール返却時のコールバックを設定する。WaveDirector が生成直後に呼び出す。
        /// </summary>
        public void SetReturnCallback(Action<EnemyController> callback)
        {
            _returnCallback = callback;
        }

        /// <summary>
        /// 敵を初期化する。WaveDirector やスポナーから呼び出す想定。
        /// EnemyData のパラメータから Model を構築し、BehaviorType で攻撃方式を自動選択する。
        /// DeathState が Agent 停止・Collider 無効化を行うため、再利用時にここでリセットする。
        /// </summary>
        public void Initialize(EnemyData data, Transform playerTransform, IDamageable playerDamageable)
        {
            Data = data;
            PlayerTransform = playerTransform;
            PlayerDamageable = playerDamageable;
            SpawnPosition = transform.position;

            Agent = GetComponent<NavMeshAgent>();
            Animator = GetComponentInChildren<Animator>();
            _collider = GetComponent<Collider>();

            // DeathState が無効化した状態をリセット（プール再利用時に必要）
            Agent.isStopped = false;
            Agent.ResetPath();
            Agent.speed = data.MoveSpeed;
            SetColliderEnabled(true);
            _lookAtWeight = 0f;

            // EnemyModel は readonly _maxHealth を持つため、データが変わる場合は毎回 new する
            Model = new EnemyModel(data.MaxHealth, data.StaggerThreshold);
            AttackBehavior = SelectBehavior(data.BehaviorType);

            _hpBarView = GetComponentInChildren<EnemyHPBarView>();
            _stateMachine = new EnemyStateMachine(this);
            RegisterStates();
            SubscribeModelEvents();
            _hpBarView?.Initialize(Model);

            _stateMachine.ChangeState(EnemyState.Spawn).Forget();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        /// <summary>
        /// Generic リグのため OnAnimatorIK / SetLookAtPosition は使用不可。
        /// Animator がボーン回転を確定した後に上書きし、
        /// Chase・Attack 中のみ頭をプレイヤー方向に向ける。
        /// </summary>
        private void LateUpdate()
        {
            ApplyHeadLookAt();
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

        public void PlayAnimation(int stateHash, float transitionDuration = DefaultTransitionDuration)
        {
            if (Animator != null)
            {
                Animator.CrossFadeInFixedTime(stateHash, transitionDuration);
            }
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }

        /// <summary>
        /// プール返却処理。イベント購読を解除し、ステートマシンをクリアしてから
        /// コールバック経由で ObjectPool に返却する。
        /// </summary>
        public void ReturnToPool()
        {
            _hpBarView?.Cleanup();
            UnsubscribeModelEvents();
            _stateMachine = null;

            if (_returnCallback != null)
            {
                _returnCallback.Invoke(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // --- ヘッドルック ---

        private void ApplyHeadLookAt()
        {
            if (_headBone == null || _stateMachine == null || PlayerTransform == null) return;

            // Death・Stagger ではアニメーションを完全に優先する。
            // 補間を待つと死亡演出の頭ボーンが上書きされ、アニメーションが停止して見える。
            EnemyState current = _stateMachine.CurrentState;
            if (current == EnemyState.Death || current == EnemyState.Stagger)
            {
                _lookAtWeight = 0f;
                return;
            }

            bool isTrackingState = current == EnemyState.Chase
                                || current == EnemyState.Attack;

            Vector3 directionToPlayer = (PlayerTransform.position - _headBone.position).normalized;
            float angleFromForward = Vector3.Angle(transform.forward, directionToPlayer);

            // 追跡ステートかつ体の正面から MaxHeadAngle 以内の場合のみ頭を向ける。
            // 角度が範囲外の場合は weight を 0 に戻し、首が不自然に回りすぎるのを防ぐ。
            float targetWeight = (isTrackingState && angleFromForward <= MaxHeadAngle) ? 1f : 0f;
            _lookAtWeight = Mathf.MoveTowards(_lookAtWeight, targetWeight, LookAtSpeed * Time.deltaTime);

            if (_lookAtWeight <= 0f) return;

            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            _headBone.rotation = Quaternion.Slerp(_headBone.rotation, lookRotation, _lookAtWeight);
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
            _stateMachine.RegisterState(EnemyState.Patrol, new PatrolState());
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
