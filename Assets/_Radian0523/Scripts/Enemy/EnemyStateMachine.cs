using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Velora.Enemy
{
    public enum EnemyState
    {
        Spawn,
        Idle,
        Chase,
        Attack,
        Stagger,
        Death
    }

    /// <summary>
    /// 敵AI のステートマシン。GameFlowManager と同一パターン。
    /// 各ステートの Enter/Update/Exit を管理し、非同期遷移をガードする。
    /// </summary>
    public class EnemyStateMachine
    {
        private readonly EnemyController _controller;
        private readonly Dictionary<EnemyState, EnemyStateBase> _states = new();
        private EnemyStateBase _currentState;
        private bool _isTransitioning;

        public EnemyController Controller => _controller;
        public EnemyState CurrentState { get; private set; }

        public EnemyStateMachine(EnemyController controller)
        {
            _controller = controller;
        }

        public void RegisterState(EnemyState state, EnemyStateBase stateInstance)
        {
            stateInstance.SetStateMachine(this);
            _states[state] = stateInstance;
        }

        /// <summary>
        /// ステートを遷移する。前ステートの Exit → 新ステートの Enter を
        /// 非同期で順番に実行し、演出の完了を待ってから次に進む。
        /// </summary>
        public async UniTask ChangeState(EnemyState newState)
        {
            if (_isTransitioning) return;
            if (!_states.ContainsKey(newState))
            {
                throw new InvalidOperationException(
                    $"EnemyState {newState} is not registered. Call RegisterState first.");
            }

            _isTransitioning = true;

            if (_currentState != null)
            {
                await _currentState.Exit();
            }

            CurrentState = newState;
            _currentState = _states[newState];

            await _currentState.Enter();

            _isTransitioning = false;
        }

        public void Update()
        {
            if (!_isTransitioning)
            {
                _currentState?.Update();
            }
        }
    }
}
