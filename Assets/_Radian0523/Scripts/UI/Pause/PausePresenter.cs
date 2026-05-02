using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// PauseManager と PauseMenuView の橋渡し。
    /// InputAction（Pause / Cancel）のイベントを受け取り、
    /// ポーズ状態に応じて View の表示切替・カーソル制御を行う。
    /// </summary>
    public class PausePresenter : MonoBehaviour
    {
        [SerializeField] private PauseMenuView _view;
        [SerializeField] private InputActionReference _pauseAction;

        private PauseManager _pauseManager;

        public PauseManager PauseManager => _pauseManager;

        private void Awake()
        {
            _pauseManager = new PauseManager();
        }

        private void OnEnable()
        {
            if (_pauseAction != null && _pauseAction.action != null)
            {
                _pauseAction.action.Enable();
                _pauseAction.action.performed += HandlePauseInput;
            }

            _pauseManager.OnPauseStateChanged += HandlePauseStateChanged;
            _view.OnResumeClicked += HandleResumeClicked;
            _view.OnTitleClicked += HandleTitleClicked;
        }

        private void OnDisable()
        {
            if (_pauseAction != null && _pauseAction.action != null)
            {
                _pauseAction.action.performed -= HandlePauseInput;
            }

            _pauseManager.OnPauseStateChanged -= HandlePauseStateChanged;
            _view.OnResumeClicked -= HandleResumeClicked;
            _view.OnTitleClicked -= HandleTitleClicked;
        }

        private void HandlePauseInput(InputAction.CallbackContext context)
        {
            _pauseManager.TogglePause();
        }

        private void HandlePauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                _view.Show();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                _view.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleResumeClicked()
        {
            _pauseManager.SetPaused(false);
        }

        private void HandleTitleClicked()
        {
            _pauseManager.SetPaused(false);
            TransitionToTitle().Forget();
        }

        private async UniTaskVoid TransitionToTitle()
        {
            if (CommonUIDirector.Instance != null)
            {
                await CommonUIDirector.Instance.TransitionToScene("TitleScene", "BattleScene");
            }
        }
    }
}
