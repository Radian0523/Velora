using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 4本線クロスヘアと着弾ヒットマーカーを制御する View 層。
    /// 拡散状態に応じて4本の RectTransform を動かすことで
    /// 命中精度を視覚的にフィードバックする。
    /// ヒットマーカーは DOTween でフェードアウトし、残像感を演出する。
    /// </summary>
    public class CrosshairView : MonoBehaviour
    {
        [Header("クロスヘア 4本線")]
        [SerializeField] private RectTransform _lineTop;
        [SerializeField] private RectTransform _lineBottom;
        [SerializeField] private RectTransform _lineLeft;
        [SerializeField] private RectTransform _lineRight;

        [Header("ヒットマーカー")]
        [SerializeField] private Image _hitMarker;

        [Header("拡散設定")]
        [SerializeField] private float _baseSpread = 10f;
        [SerializeField] private float _maxSpread = 50f;

        [Header("ヒットマーカー設定")]
        [SerializeField] private float _hitMarkerDuration = 0.25f;

        private Tween _hitMarkerTween;

        private void Awake()
        {
            // 初期状態では非表示
            if (_hitMarker != null)
            {
                var color = _hitMarker.color;
                color.a = 0f;
                _hitMarker.color = color;
            }

            // 初期拡散をゼロに設定
            UpdateSpread(0f);
        }

        /// <summary>
        /// クロスヘアの拡散幅を更新する。normalized は 0〜1 の値で渡す。
        /// WeaponController の移動速度・射撃後の拡散量から算出して渡すことを想定。
        /// </summary>
        public void UpdateSpread(float normalized)
        {
            float spread = Mathf.Lerp(_baseSpread, _maxSpread, Mathf.Clamp01(normalized));
            SetLinePosition(_lineTop,    new Vector2(0f,     spread));
            SetLinePosition(_lineBottom, new Vector2(0f,    -spread));
            SetLinePosition(_lineLeft,   new Vector2(-spread, 0f));
            SetLinePosition(_lineRight,  new Vector2(spread,  0f));
        }

        /// <summary>
        /// ヒットマーカーを一瞬表示してフェードアウトする。
        /// ヘッドショット時は赤色にすることで視覚的な区別を与える。
        /// </summary>
        public void ShowHitMarker(bool isHeadshot)
        {
            if (_hitMarker == null) return;

            _hitMarkerTween?.Kill();

            var color = isHeadshot ? Color.red : Color.white;
            color.a = 1f;
            _hitMarker.color = color;

            _hitMarkerTween = _hitMarker
                .DOFade(0f, _hitMarkerDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// ADS（エイム）時はクロスヘアを非表示にする。
        /// ADS 中はスコープ越しの精密射撃を想定し、クロスヘアが邪魔になるため。
        /// </summary>
        public void SetAdsMode(bool isAds)
        {
            gameObject.SetActive(!isAds);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        private void SetLinePosition(RectTransform line, Vector2 anchoredPosition)
        {
            if (line != null)
            {
                line.anchoredPosition = anchoredPosition;
            }
        }
    }
}
