using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// ジェネリックステートマシン。
    /// GameFlowManager（ゲームフロー）と EnemyStateMachine（敵AI）の共通ロジックを
    /// 一箇所に集約する。登録 → 遷移（Exit → Enter）→ 毎フレーム Update の
    /// ライフサイクルを管理し、非同期遷移中の重複呼び出しをガードする。
    ///
    /// 新しいステートマシンが必要になった場合、このクラスを継承し
    /// InitializeState で固有の初期化を差し込むだけで対応できる。
    /// </summary>
    public class StateMachine<TEnum, TState> where TEnum : Enum where TState : class, IState
    {
        private readonly Dictionary<TEnum, TState> _states = new();
        private TState _currentState;
        private bool _isTransitioning;

        public TEnum CurrentState { get; private set; }
        public event Action<TEnum> OnStateChanged;

        /// <summary>
        /// ステート登録時の初期化フック。
        /// サブクラスがステートにオーナー参照を渡す等の固有処理を差し込む。
        /// </summary>
        protected virtual void InitializeState(TState stateInstance) { }

        public void RegisterState(TEnum state, TState stateInstance)
        {
            InitializeState(stateInstance);
            _states[state] = stateInstance;
        }

        /// <summary>
        /// ステートを遷移する。前ステートの Exit → 新ステートの Enter を
        /// 非同期で順番に実行し、演出の完了を待ってから次に進む。
        /// </summary>
        public async UniTask ChangeState(TEnum newState)
        {
            if (_isTransitioning) return;
            if (!_states.ContainsKey(newState))
            {
                throw new InvalidOperationException(
                    $"State {newState} is not registered. Call RegisterState first.");
            }

            _isTransitioning = true;

            if (_currentState != null)
            {
                await _currentState.Exit();
            }

            CurrentState = newState;
            _currentState = _states[newState];
            OnStateChanged?.Invoke(newState);

            await _currentState.Enter();

            _isTransitioning = false;
        }

        /// <summary>
        /// 現在のステートの Update を呼び出す。
        /// MonoBehaviour の Update から毎フレーム呼び出す想定。
        /// </summary>
        public void Update()
        {
            if (!_isTransitioning)
            {
                _currentState?.Update();
            }
        }
    }
}
