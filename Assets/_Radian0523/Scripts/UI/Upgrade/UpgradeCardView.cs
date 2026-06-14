using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Velora.Data;

namespace Velora.UI
{
    /// <summary>
    /// アップグレード選択カード 1 枚の View。
    /// ホバー時のスケール拡大で操作フィードバックを与える。
    /// レアリティ別カラーでカードの価値を視覚的に区別する。
    /// </summary>
    public class UpgradeCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("カード要素")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Button _selectButton;

        [Header("レアリティカラー")]
        [SerializeField] private Color _commonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _rareColor   = new Color(0.2f, 0.4f, 0.9f, 1f);
        [SerializeField] private Color _epicColor   = new Color(0.7f, 0.2f, 0.9f, 1f);

        private const float HoverScale = 1.05f;
        private const float HoverDuration = 0.15f;

        private UpgradeData _upgradeData;
        private Tween _hoverTween;

        public event Action<UpgradeData> OnSelected;

        private void Awake()
        {
            _selectButton.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// カードにアップグレードデータをバインドする。
        /// UpgradeSelectView が DisplayChoices() 内で各カードに呼び出す。
        /// </summary>
        public void Setup(UpgradeData data)
        {
            _upgradeData = data;
            _nameText.text = data.UpgradeName;
            _descriptionText.text = data.Description;
            _rarityText.text = data.Rarity.ToString().ToUpper();

            if (_icon != null)
            {
                _icon.sprite = data.Icon;
                _icon.gameObject.SetActive(data.Icon != null);
            }

            Color rarityColor = data.Rarity switch
            {
                UpgradeRarity.Common => _commonColor,
                UpgradeRarity.Rare   => _rareColor,
                UpgradeRarity.Epic   => _epicColor,
                _                    => _commonColor
            };

            if (_background != null)
            {
                _background.color = rarityColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverScale(HoverScale, Ease.OutBack);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayHoverScale(1f, Ease.OutQuad);
        }

        /// <summary>
        /// ホバーのスケール tween を再生する。
        /// カードはウェーブ毎に生成/破棄されるため、前の tween を Kill してから再生し、
        /// ホバー中に破棄されても tween が transform を掴み続けないようにする。
        /// </summary>
        private void PlayHoverScale(float targetScale, Ease ease)
        {
            _hoverTween?.Kill();
            _hoverTween = transform.DOScale(targetScale, HoverDuration).SetEase(ease);
        }

        private void HandleClick()
        {
            OnSelected?.Invoke(_upgradeData);
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
        }
    }
}
