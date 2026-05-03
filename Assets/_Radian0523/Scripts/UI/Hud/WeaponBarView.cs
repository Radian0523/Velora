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
        private int _assignedCount;

        /// <summary>
        /// 全スロットを空状態にリセットする。
        /// Scene に配置済みのスロットをそのまま使い、Instantiate は行わない。
        /// </summary>
        public void Initialize()
        {
            _assignedCount = 0;
            _currentSelectedIndex = -1;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].SetupEmpty(i + 1);
            }
        }

        /// <summary>
        /// 次の空スロットに武器アイコンを割り当てる。
        /// 呼び出し順で左から順に埋まっていく。
        /// </summary>
        public void AssignWeapon(Sprite icon)
        {
            if (_assignedCount >= _slots.Length) return;

            _slots[_assignedCount].AssignWeapon(icon);
            _assignedCount++;
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _assignedCount) return;

            if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _slots.Length)
            {
                _slots[_currentSelectedIndex].SetSelected(false);
            }

            _slots[index].SetSelected(true);
            _currentSelectedIndex = index;
        }
    }
}
