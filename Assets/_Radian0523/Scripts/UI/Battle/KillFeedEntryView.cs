using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Velora.UI
{
    /// <summary>
    /// キルフィードの個別エントリ View。
    /// 敵名とスコアを表示し、一定時間後にフェードアウトしてプール返却する。
    /// ObjectPool から取得・返却されることを前提とした設計。
    /// </summary>
    public class KillFeedEntryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float DisplayDuration = 3f;
        private const float FadeDuration = 0.5f;

        private Action<KillFeedEntryView> _returnToPool;

        public void SetReturnCallback(Action<KillFeedEntryView> callback)
        {
            _returnToPool = callback;
        }

        /// <summary>
        /// エントリを表示する。前回の DOTween をキャンセルしてから新しい演出を開始する。
        /// DisplayDuration 経過後にフェードアウトし、完了後にプールへ返却する。
        /// </summary>
        public void Show(string enemyName, int score)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 1f;
            _text.text = $"{enemyName}  +{score}";

            DOVirtual.DelayedCall(DisplayDuration, () =>
            {
                _canvasGroup.DOFade(0f, FadeDuration)
                    .OnComplete(() => _returnToPool?.Invoke(this));
            });
        }
    }
}
