using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// PauseManager と PauseMenuView の橋渡し。
    /// InputAction（Pause / Cancel）のイベントを受け取り、
    /// ポーズ状態に応じて View の表示切替・カーソル制御を行う。
    /// 設定画面の開閉も管理し、PauseMenu ↔ Settings の遷移を制御する。
    /// </summary>
    public class PausePresenter : MonoBehaviour
    {
        [SerializeField] private PauseMenuView _view;
        [SerializeField] private InputActionReference _pauseAction;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        private PauseManager _pauseManager;
        private bool _isSettingsOpen;
        private SceneNavigator _sceneNavigator;

        [Inject]
        public void Construct(SceneNavigator sceneNavigator)
        {
            _sceneNavigator = sceneNavigator;
        }

        public PauseManager PauseManager => _pauseManager;
        public SettingsPresenter SettingsPresenter => _settingsPresenter;

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
            _view.OnSettingsClicked += HandleSettingsClicked;
            _view.OnTitleClicked += HandleTitleClicked;

            if (_settingsPresenter != null)
            {
                _settingsPresenter.GetComponent<SettingsView>()
                    .OnBackClicked += HandleSettingsBackClicked;
            }
        }

        private void OnDisable()
        {
            if (_pauseAction != null && _pauseAction.action != null)
            {
                _pauseAction.action.performed -= HandlePauseInput;
            }

            _pauseManager.OnPauseStateChanged -= HandlePauseStateChanged;
            _view.OnResumeClicked -= HandleResumeClicked;
            _view.OnSettingsClicked -= HandleSettingsClicked;
            _view.OnTitleClicked -= HandleTitleClicked;

            if (_settingsPresenter != null)
            {
                _settingsPresenter.GetComponent<SettingsView>()
                    .OnBackClicked -= HandleSettingsBackClicked;
            }
        }

        private void HandlePauseInput(InputAction.CallbackContext context)
        {
            // 設定画面が開いている場合は ESC でポーズメニューに戻る
            if (_isSettingsOpen)
            {
                CloseSettings();
                return;
            }

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
                // Unpause 時はポーズメニューと設定画面の両方を閉じる
                _view.Hide();
                if (_isSettingsOpen)
                {
                    _settingsPresenter?.Hide();
                    _isSettingsOpen = false;
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleResumeClicked()
        {
            _pauseManager.SetPaused(false);
        }

        private void HandleSettingsClicked()
        {
            _view.Hide();
            _settingsPresenter?.Show();
            _isSettingsOpen = true;
        }

        private void HandleSettingsBackClicked()
        {
            CloseSettings();
        }

        private void CloseSettings()
        {
            _settingsPresenter?.Hide();
            _isSettingsOpen = false;
            _view.Show();
        }

        private void HandleTitleClicked()
        {
            _pauseManager.SetPaused(false);
            TransitionToTitle().Forget();
        }

        private async UniTaskVoid TransitionToTitle()
        {
            await _sceneNavigator.TransitionTo("Title", "Battle");
        }
    }
}
