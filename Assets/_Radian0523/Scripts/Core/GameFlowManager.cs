using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    public enum GameState
    {
        Title,
        BattleReady,
        BattleInProgress,
        WaveCleared,
        UpgradeSelect,
        GameOver,
        Result
    }

    /// <summary>
    /// ゲーム全体のフローをステートマシンで管理する pure C# クラス。
    /// 各ステートが Enter/Update/Exit で自分の責務を完結させることで、
    /// 新しいステートの追加がクラス1つの作成で完結する。
    /// </summary>
    public class GameFlowManager
    {
        private readonly Dictionary<GameState, GameStateBase> _states = new();
        private GameStateBase _currentState;
        private bool _isTransitioning;

        public GameState CurrentState { get; private set; }
        public event Action<GameState> OnStateChanged;

        /// <summary>
        /// ステートを登録する。ゲーム初期化時に全ステートを登録しておく。
        /// </summary>
        public void RegisterState(GameState state, GameStateBase stateInstance)
        {
            stateInstance.SetOwner(this);
            _states[state] = stateInstance;
        }

        /// <summary>
        /// ステートを遷移する。前ステートの Exit → 新ステートの Enter を
        /// 非同期で順番に実行し、演出の完了を待ってから次に進む。
        /// </summary>
        public async UniTask ChangeState(GameState newState)
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
