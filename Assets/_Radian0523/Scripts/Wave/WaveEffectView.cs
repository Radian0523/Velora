using UnityEngine;
using DG.Tweening;
using TMPro;
using Cysharp.Threading.Tasks;

namespace Velora.Wave
{
    /// <summary>
    /// ウェーブ開始・クリア時の演出 View。
    /// Presenter（BattleSceneDirector）から呼び出され、テキスト表示のみを担当する。
    /// DOTween + UniTask で演出シーケンスを async/await で記述し可読性を確保する。
    /// </summary>
    public class WaveEffectView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _waveClearText;

        private const float ScaleAnimationDuration = 0.4f;
        private const float FadeAnimationDuration = 0.3f;
        private const float WaveStartHoldDuration = 0.8f;
        private const float WaveClearHoldDuration = 1f;

        private void Awake()
        {
            SetTextAlpha(_waveText, 0f);
            SetTextAlpha(_waveClearText, 0f);
            _waveText.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// 「Wave N」をスケールアニメーションで表示し、一定時間保持後にフェードアウトする。
        /// GameState の BattleReady で await されるため、演出完了まで次の処理が待機する。
        /// </summary>
        public async UniTask PlayWaveStartSequence(int waveNumber)
        {
            _waveText.text = $"Wave {waveNumber}";
            _waveText.transform.localScale = Vector3.zero;
            SetTextAlpha(_waveText, 1f);

            // OutBack でポップイン感のある登場演出
            await _waveText.transform
                .DOScale(Vector3.one, ScaleAnimationDuration)
                .SetEase(Ease.OutBack)
                .AsyncWaitForCompletion();

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(WaveStartHoldDuration));

            await _waveText
                .DOFade(0f, FadeAnimationDuration)
                .AsyncWaitForCompletion();

            _waveText.transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// 「Wave N Cleared!」をフェードイン→保持→フェードアウトで表示する。
        /// </summary>
        public async UniTask PlayWaveClearSequence(int waveNumber)
        {
            _waveClearText.text = $"Wave {waveNumber} Cleared!";
            SetTextAlpha(_waveClearText, 0f);

            await _waveClearText
                .DOFade(1f, FadeAnimationDuration)
                .AsyncWaitForCompletion();

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(WaveClearHoldDuration));

            await _waveClearText
                .DOFade(0f, FadeAnimationDuration)
                .AsyncWaitForCompletion();
        }

        private void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            var color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
