using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// クロスヘア周囲のリロード進捗リングを制御する View。
    /// WeaponController に進捗イベントを追加せず、開始時に duration を受け取り
    /// DOTween の時間ベースアニメーションで fillAmount を駆動する設計。
    /// これにより Model/Controller 側の変更なしに視覚フィードバックを追加できる。
    /// </summary>
    public class ReloadRingView : MonoBehaviour
    {
        [SerializeField] private Image _ringFill;
        [SerializeField] private float _fadeOutDuration = 0.15f;

        private Tween _fillTween;
        private Tween _fadeTween;

        /// <summary>
        /// リロード進捗リングを開始する。
        /// duration 秒かけて fillAmount を 0→1 に充填し、完了後フェードアウトする。
        /// </summary>
        public void StartFill(float duration)
        {
            Cancel();
            gameObject.SetActive(true);

            _ringFill.fillAmount = 0f;
            SetAlpha(1f);

            _fillTween = DOTween.To(
                () => _ringFill.fillAmount,
                x => _ringFill.fillAmount = x,
                1f,
                duration
            ).SetEase(Ease.Linear).OnComplete(FadeOut);
        }

        /// <summary>
        /// リロードキャンセル時（武器切替など）に即座にリングを非表示にする。
        /// 実行中の Tween を確実に停止してから非表示にすることでゴーストアニメーションを防ぐ。
        /// </summary>
        public void Cancel()
        {
            _fillTween?.Kill();
            _fillTween = null;
            _fadeTween?.Kill();
            _fadeTween = null;
            gameObject.SetActive(false);
        }

        private void FadeOut()
        {
            _fillTween = null;
            _fadeTween = _ringFill
                .DOFade(0f, _fadeOutDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void SetAlpha(float alpha)
        {
            var color = _ringFill.color;
            color.a = alpha;
            _ringFill.color = color;
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
