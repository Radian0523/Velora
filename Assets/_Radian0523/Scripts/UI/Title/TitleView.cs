using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// タイトル画面の View 層。
    /// START ボタンのクリックイベントを Presenter に委譲する。
    /// CanvasGroup による表示/非表示のフェードアニメーションを提供する。
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float FadeDuration = 0.5f;

        public event Action OnStartClicked;

        private void Awake()
        {
            _startButton.onClick.AddListener(() => OnStartClicked?.Invoke());
        }

        public void SetStartButtonInteractable(bool interactable)
        {
            _startButton.interactable = interactable;
        }

        public void Show()
        {
            _canvasGroup.DOFade(1f, FadeDuration)
                .SetUpdate(true);
        }

        public void Hide()
        {
            _canvasGroup.DOFade(0f, FadeDuration)
                .SetUpdate(true);
        }
    }
}
