using System;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// ポーズ状態を管理する pure C# クラス。
    /// Time.timeScale を制御し、ポーズ中は物理演算・Update 系を停止する。
    /// FadeView や PauseMenuView は SetUpdate(true) で timeScale=0 でも動作する。
    /// </summary>
    public class PauseManager
    {
        private bool _isPaused;

        public bool IsPaused => _isPaused;
        public event Action<bool> OnPauseStateChanged;

        public void TogglePause()
        {
            SetPaused(!_isPaused);
        }

        /// <summary>
        /// ポーズ状態を明示的に設定する。
        /// シーン遷移前にポーズを解除する場合など、トグルではなく確定値を指定したい場合に使用。
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;

            _isPaused = paused;
            Time.timeScale = _isPaused ? 0f : 1f;
            OnPauseStateChanged?.Invoke(_isPaused);
        }
    }
}
