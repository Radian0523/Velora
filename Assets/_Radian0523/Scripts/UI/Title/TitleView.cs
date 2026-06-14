using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
            // Show/Hide 連打での tween 競合を防ぐため、直前の tween を必ず止めてから開始する。
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(1f, FadeDuration)
                .SetUpdate(true);
        }

        public void Hide()
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, FadeDuration)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            // 破棄後に tween が CanvasGroup を掴み続けないよう解放する。
            _canvasGroup.DOKill();
        }
    }
}
