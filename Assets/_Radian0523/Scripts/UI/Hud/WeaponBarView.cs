using UnityEngine;

namespace Velora.UI
{
    /// <summary>
    /// 武器バー全体のコンテナ View。
    /// スロットは Scene 上に事前配置し、Inspector から参照する。
    /// Editor 上でレイアウトを確認・調整できるため、見た目の調整が容易。
    /// 武器取得時に左から順にアイコンを割り当てる。
    /// </summary>
    public class WeaponBarView : MonoBehaviour
    {
        [SerializeField] private WeaponSlotView[] _slots;

        private int _currentSelectedIndex = -1;

        /// <summary>
        /// 全スロットを空状態にリセットする。
        /// Scene に配置済みのスロットをそのまま使い、Instantiate は行わない。
        /// </summary>
        public void Initialize()
        {
            _currentSelectedIndex = -1;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetupEmpty(i + 1);
            }
        }

        /// <summary>
        /// 指定スロットに武器アイコンを割り当てる。
        /// WeaponData.SlotIndex で決まる固定位置に配置することで、
        /// キー入力（1-5）との対応を常に一致させる。
        /// </summary>
        public void AssignWeapon(int slotIndex, Sprite icon)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return;

            _slots[slotIndex].AssignWeapon(icon);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return;

            if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _slots.Length)
            {
                _slots[_currentSelectedIndex].SetSelected(false);
            }

            _slots[index].SetSelected(true);
            _currentSelectedIndex = index;
        }
    }
}
