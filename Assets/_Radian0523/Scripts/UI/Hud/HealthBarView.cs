using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// HP バーの演出を担当する View。
    /// Slider ではなく Image.fillAmount ベースの2層構造により、
    /// ダメージトレイル・色変化・数値カウント・瀕死パルスの4演出を実現する。
    /// Presenter から UpdateHealth() を呼ぶだけで全演出が連動する。
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        [Header("バー")]
        [SerializeField] private Image _mainFill;
        [SerializeField] private Image _trailFill;

        [Header("テキスト")]
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("色設定")]
        [SerializeField] private Color _healthyColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(0.95f, 0.85f, 0.1f);
        [SerializeField] private Color _criticalColor = new Color(0.9f, 0.15f, 0.15f);
        [SerializeField] private float _warningThreshold = 0.5f;
        [SerializeField] private float _criticalThreshold = 0.25f;

        [Header("アニメーション")]
        [SerializeField] private float _mainBarDuration = 0.3f;
        [SerializeField] private float _trailDelay = 0.5f;
        [SerializeField] private float _trailDuration = 0.6f;
        [SerializeField] private float _numberCountDuration = 0.4f;

        [Header("瀕死演出")]
        [SerializeField] private float _criticalPulseDuration = 0.6f;

        private float _displayedNumber;
        private float _maxHealth;
        private bool _isInitialized;
        private Tween _mainTween;
        private Tween _trailTween;
        private Tween _numberTween;
        private Tween _criticalTween;

        /// <summary>
        /// HP バーを更新する。Presenter から呼ばれるたびに
        /// メインバー・トレイル・色・数値・瀕死パルスの全演出を連動更新する。
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            _maxHealth = max;
            float normalized = max > 0f ? current / max : 0f;

            if (!_isInitialized)
            {
                InitializeImmediate(current, max, normalized);
                return;
            }

            float previousFill = _mainFill.fillAmount;
            bool isDamage = normalized < previousFill;

            AnimateMainBar(normalized);
            UpdateBarColor(normalized);
            AnimateTrail(normalized, isDamage);
            AnimateNumber(current);
            UpdateCriticalPulse(normalized);
        }

        /// <summary>
        /// 初回呼び出し時はアニメーションなしで即座に全要素を設定する。
        /// ゲーム開始時に数値がゼロからカウントアップする不自然さを防ぐ。
        /// </summary>
        private void InitializeImmediate(float current, float max, float normalized)
        {
            _mainFill.fillAmount = normalized;
            _trailFill.fillAmount = normalized;
            _mainFill.color = EvaluateHealthColor(normalized);
            _displayedNumber = current;
            _healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            _isInitialized = true;
        }

        private void AnimateMainBar(float normalized)
        {
            _mainTween?.Kill();
            _mainTween = DOTween.To(
                () => _mainFill.fillAmount,
                x => _mainFill.fillAmount = x,
                normalized,
                _mainBarDuration
            ).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// HP 割合に応じて 緑→黄→赤 のグラデーション色を即時設定する。
        /// fillAmount のアニメーションとは独立させることで、
        /// 瀕死パルスの alpha 制御と干渉しない設計にしている。
        /// </summary>
        private void UpdateBarColor(float normalized)
        {
            Color color = EvaluateHealthColor(normalized);

            if (_criticalTween != null)
            {
                color.a = _mainFill.color.a;
            }

            _mainFill.color = color;
        }

        private void AnimateTrail(float normalized, bool isDamage)
        {
            _trailTween?.Kill();

            if (isDamage)
            {
                // ダメージ時: 旧 fillAmount を維持した後、遅延して追従する。
                // プレイヤーに「どれだけ減ったか」を視覚的に認識させる演出。
                _trailTween = DOTween.To(
                    () => _trailFill.fillAmount,
                    x => _trailFill.fillAmount = x,
                    normalized,
                    _trailDuration
                ).SetEase(Ease.InQuad).SetDelay(_trailDelay);
            }
            else
            {
                // 回復時: トレイルがメインより前に出る不自然な表示を防ぐため即追従
                _trailFill.fillAmount = normalized;
            }
        }

        private void AnimateNumber(float current)
        {
            _numberTween?.Kill();
            _numberTween = DOTween.To(
                () => _displayedNumber,
                x =>
                {
                    _displayedNumber = x;
                    _healthText.text = $"{Mathf.CeilToInt(x)} / {Mathf.CeilToInt(_maxHealth)}";
                },
                current,
                _numberCountDuration
            ).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// HP 割合から 緑→黄→赤 のグラデーション色を返す。
        /// warning/critical の2段階閾値で Lerp することで、
        /// HP の残量が直感的に読み取れる色変化を実現する。
        /// </summary>
        private Color EvaluateHealthColor(float normalized)
        {
            if (normalized > _warningThreshold)
            {
                float t = Mathf.InverseLerp(1f, _warningThreshold, normalized);
                return Color.Lerp(_healthyColor, _warningColor, t);
            }

            float t2 = Mathf.InverseLerp(_warningThreshold, _criticalThreshold, normalized);
            return Color.Lerp(_warningColor, _criticalColor, t2);
        }

        /// <summary>
        /// 瀕死状態でバーの alpha をパルスさせることで緊迫感を演出する。
        /// 回復で閾値を超えたらパルスを停止し alpha を完全不透明に復帰させる。
        /// </summary>
        private void UpdateCriticalPulse(float normalized)
        {
            bool isCritical = normalized <= _criticalThreshold && normalized > 0f;

            if (isCritical)
            {
                if (_criticalTween != null) return;

                _criticalTween = _mainFill
                    .DOFade(0.4f, _criticalPulseDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else if (_criticalTween != null)
            {
                _criticalTween.Kill();
                _criticalTween = null;

                Color color = _mainFill.color;
                color.a = 1f;
                _mainFill.color = color;
            }
        }

        private void OnDestroy()
        {
            _mainTween?.Kill();
            _trailTween?.Kill();
            _numberTween?.Kill();
            _criticalTween?.Kill();
        }
    }
}
