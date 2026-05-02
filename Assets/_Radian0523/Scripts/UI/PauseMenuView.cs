using System;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// ポーズメニューの View 層。
    /// CanvasGroup の alpha/interactable で表示・非表示を切り替え、
    /// ボタンクリックはイベントで Presenter に委譲する。
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _titleButton;

        public event Action OnResumeClicked;
        public event Action OnTitleClicked;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
            _titleButton.onClick.AddListener(() => OnTitleClicked?.Invoke());
            Hide();
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
    }
}
