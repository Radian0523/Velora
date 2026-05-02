using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// 武器バーの個別スロット。アイコン・番号・選択状態を表示する。
    /// 初期状態では空スロット（番号のみ・半透明）として表示され、
    /// 武器が割り当てられるとアイコンが表示される。
    /// ロジックは持たず、見た目の制御のみ行う View 層。
    /// </summary>
    public class WeaponSlotView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _selectedFrame;
        [SerializeField] private TextMeshProUGUI _numberText;
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float SelectedAlpha = 1f;
        private const float UnselectedAlpha = 0.5f;
        private const float EmptyAlpha = 0.3f;

        private bool _hasWeapon;

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
        /// 空スロットから武器所持状態に遷移し、通常の半透明表示になる。
        /// </summary>
        public void AssignWeapon(Sprite icon)
        {
            _hasWeapon = true;

            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = true;
            }

            _canvasGroup.alpha = UnselectedAlpha;
        }

        public void SetSelected(bool isSelected)
        {
            if (!_hasWeapon) return;

            _selectedFrame.enabled = isSelected;
            _canvasGroup.alpha = isSelected ? SelectedAlpha : UnselectedAlpha;
        }
    }
}
