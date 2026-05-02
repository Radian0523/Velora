using System.Collections.Generic;
using UnityEngine;

namespace Velora.UI
{
    /// <summary>
    /// 武器バー全体のコンテナ View。
    /// 固定数の空スロットを初期生成し、武器取得時に左から順にアイコンを割り当てる。
    /// スロット枠は常に表示されているため、プレイヤーに「まだ武器を拾える」ことを
    /// 視覚的に伝えられる。
    /// </summary>
    public class WeaponBarView : MonoBehaviour
    {
        [SerializeField] private WeaponSlotView _slotPrefab;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private int _slotCount = 5;

        private readonly List<WeaponSlotView> _slots = new();
        private int _currentSelectedIndex = -1;
        private int _assignedCount;

        /// <summary>
        /// 空スロットを _slotCount 個生成する。
        /// 武器の割り当ては AssignWeapon() で後から行う。
        /// </summary>
        public void Initialize()
        {
            ClearSlots();

            for (int i = 0; i < _slotCount; i++)
            {
                var slot = Instantiate(_slotPrefab, _slotContainer);
                slot.SetupEmpty(i + 1);
                _slots.Add(slot);
            }

            _assignedCount = 0;
        }

        /// <summary>
        /// 次の空スロットに武器アイコンを割り当てる。
        /// 呼び出し順で左から順に埋まっていく。
        /// </summary>
        public void AssignWeapon(Sprite icon)
        {
            if (_assignedCount >= _slots.Count) return;

            _slots[_assignedCount].AssignWeapon(icon);
            _assignedCount++;
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _assignedCount) return;

            if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _slots.Count)
            {
                _slots[_currentSelectedIndex].SetSelected(false);
            }

            _slots[index].SetSelected(true);
            _currentSelectedIndex = index;
        }

        private void ClearSlots()
        {
            foreach (var slot in _slots)
            {
                Destroy(slot.gameObject);
            }
            _slots.Clear();
            _currentSelectedIndex = -1;
            _assignedCount = 0;
        }
    }
}
