using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Velora.UI
{
    /// <summary>
    /// 全画面フェード演出の View 層。
    /// SetUpdate(true) により Time.timeScale=0（ポーズ中）でも動作する。
    /// CommonUI シーンに常駐し、CommonUIDirector 経由で各シーンからアクセスする。
    /// </summary>
    public class FadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float DefaultDuration = 0.5f;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 画面を暗転させる（alpha 0→1）。
        /// シーン遷移前に呼び出し、遷移中の画面切り替えを隠す。
        /// </summary>
        public async UniTask FadeOut(float duration = DefaultDuration)
        {
            _canvasGroup.blocksRaycasts = true;
            await _canvasGroup.DOFade(1f, duration)
                .SetUpdate(true)
                .SetEase(Ease.Linear)
                .AsyncWaitForCompletion();
        }

        /// <summary>
        /// 暗転を解除する（alpha 1→0）。
        /// シーンロード完了後に呼び出し、新シーンを表示する。
        /// </summary>
        public async UniTask FadeIn(float duration = DefaultDuration)
        {
            await _canvasGroup.DOFade(0f, duration)
                .SetUpdate(true)
                .SetEase(Ease.Linear)
                .AsyncWaitForCompletion();
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
