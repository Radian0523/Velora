using System;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 設定画面の View 層。
    /// BGM/SE 音量とマウス感度のスライダーを表示し、
    /// 値変更イベントを Presenter に委譲する。
    /// PauseMenuView と同じ CanvasGroup show/hide パターンに従う。
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _seSlider;
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private Button _backButton;

        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSeVolumeChanged;
        public event Action<float> OnMouseSensitivityChanged;
        public event Action OnBackClicked;

        // スライダー初期値設定中にイベントが発火するのを防ぐガードフラグ。
        // SetValues() で外部から値を同期する際、スライダーの onValueChanged が
        // 連鎖的に Presenter → Model へ伝搬するのを回避する。
        private bool _isSyncingSliders;

        private void Awake()
        {
            _bgmSlider.onValueChanged.AddListener(HandleBgmChanged);
            _seSlider.onValueChanged.AddListener(HandleSeChanged);
            _mouseSensitivitySlider.onValueChanged.AddListener(HandleMouseSensitivityChanged);
            _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            Hide();
        }

        /// <summary>
        /// 現在の設定値をスライダーに反映する。
        /// Presenter から呼ばれ、Model の値を View に同期する。
        /// </summary>
        public void SetValues(float bgmVolume, float seVolume, float mouseSensitivity)
        {
            _isSyncingSliders = true;
            _bgmSlider.value = bgmVolume;
            _seSlider.value = seVolume;
            _mouseSensitivitySlider.value = mouseSensitivity;
            _isSyncingSliders = false;
        }

        public void Show()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void HandleBgmChanged(float value)
        {
            if (!_isSyncingSliders) OnBgmVolumeChanged?.Invoke(value);
        }

        private void HandleSeChanged(float value)
        {
            if (!_isSyncingSliders) OnSeVolumeChanged?.Invoke(value);
        }

        private void HandleMouseSensitivityChanged(float value)
        {
            if (!_isSyncingSliders) OnMouseSensitivityChanged?.Invoke(value);
        }
    }
}
