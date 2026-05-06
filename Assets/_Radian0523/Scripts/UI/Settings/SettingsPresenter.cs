using UnityEngine;
using Velora.Core;
using Velora.Player;

namespace Velora.UI
{
    /// <summary>
    /// 設定画面の Presenter 層。
    /// SettingsView のスライダーイベントを AudioManager / FPSController に橋渡しする。
    /// FPSController は Battle シーンのみに存在するため、Initialize で遅延注入を行う。
    /// AudioManager は CommonUIDirector 経由で取得する（シーン横断サービス）。
    /// </summary>
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        private AudioManager _audioManager;
        private FPSController _fpsController;

        private void OnEnable()
        {
            _view.OnBgmVolumeChanged += HandleBgmVolumeChanged;
            _view.OnSeVolumeChanged += HandleSeVolumeChanged;
            _view.OnMouseSensitivityChanged += HandleMouseSensitivityChanged;
            _view.OnBackClicked += HandleBackClicked;
        }

        private void OnDisable()
        {
            _view.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
            _view.OnSeVolumeChanged -= HandleSeVolumeChanged;
            _view.OnMouseSensitivityChanged -= HandleMouseSensitivityChanged;
            _view.OnBackClicked -= HandleBackClicked;
        }

        /// <summary>
        /// Battle シーンから FPSController 参照を受け取る。
        /// FPSController は Battle シーンにのみ存在するため、
        /// BattleSceneDirector の初期化完了後に呼ばれる。
        /// </summary>
        public void Initialize(FPSController fpsController)
        {
            _fpsController = fpsController;
        }

        /// <summary>
        /// 設定画面を表示する際にスライダーを現在値で同期する。
        /// </summary>
        public void Show()
        {
            _audioManager = CommonUIDirector.Instance?.AudioManager;

            float bgm = _audioManager?.BgmVolume ?? 1f;
            float se = _audioManager?.SeVolume ?? 1f;
            float sensitivity = _fpsController != null ? _fpsController.MouseSensitivity : 0.15f;

            _view.SetValues(bgm, se, sensitivity);
            _view.Show();
        }

        public void Hide()
        {
            _view.Hide();
        }

        private void HandleBgmVolumeChanged(float volume)
        {
            _audioManager?.SetBGMVolume(volume);
        }

        private void HandleSeVolumeChanged(float volume)
        {
            _audioManager?.SetSEVolume(volume);
        }

        private void HandleMouseSensitivityChanged(float sensitivity)
        {
            _fpsController?.SetMouseSensitivity(sensitivity);
        }

        private void HandleBackClicked()
        {
            Hide();
        }
    }
}
