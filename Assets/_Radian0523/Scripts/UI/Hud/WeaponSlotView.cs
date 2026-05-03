using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 武器バーの個別スロット。アイコン・番号・選択状態を表示する。
    /// 選択中はスロットの背面にひと回り大きな発光矩形を表示して強調する。
    /// ロジックは持たず、見た目の制御のみ行う View 層。
    /// </summary>
    public class WeaponSlotView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _selectedFrame;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("選択枠の発光設定")]
        [SerializeField] private Color _glowColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private float _glowPulseDuration = 0.8f;
        [SerializeField] private float _glowMinAlpha = 0.4f;
        [SerializeField] private float _framePadding = 4f;

        private const float OwnedAlpha = 0.7f;
        private const float EmptyAlpha = 0.3f;

        private bool _hasWeapon;
        private Tween _glowTween;

        private void Awake()
        {
            SetupFrameRect();
        }

        /// <summary>
        /// _selectedFrame をスロット全体より _framePadding 分だけ大きく配置し、
        /// 描画順を最背面にする。スロットの裏側から少しはみ出す光枠になる。
        /// </summary>
        private void SetupFrameRect()
        {
            if (_selectedFrame == null) return;

            var frameRect = _selectedFrame.rectTransform;
            frameRect.SetAsFirstSibling();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(-_framePadding, -_framePadding);
            frameRect.offsetMax = new Vector2(_framePadding, _framePadding);
        }

        /// <summary>
        /// 空スロットとして初期化する。番号のみ表示し、アイコンは非表示。
        /// </summary>
        public void SetupEmpty(int slotNumber)
        {
            _numberText.text = slotNumber.ToString();
            _iconImage.enabled = false;
            _selectedFrame.enabled = false;
            _canvasGroup.alpha = EmptyAlpha;
            _hasWeapon = false;
        }

        /// <summary>
        /// 武器アイコンをスロットに割り当てる。
        /// 空スロットから武器所持状態に遷移する。
        /// </summary>
        public void AssignWeapon(Sprite icon)
        {
            _hasWeapon = true;

            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }

            _canvasGroup.alpha = OwnedAlpha;
        }

        public void SetSelected(bool isSelected)
        {
            if (!_hasWeapon) return;

            _glowTween?.Kill();

            if (isSelected)
            {
                _selectedFrame.color = _glowColor;
                _selectedFrame.enabled = true;
                _canvasGroup.alpha = 1f;

                _glowTween = _selectedFrame
                    .DOFade(_glowMinAlpha, _glowPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                _selectedFrame.enabled = false;
                _canvasGroup.alpha = OwnedAlpha;
            }
        }

        private void OnDestroy()
        {
            _glowTween?.Kill();
        }
    }
}
