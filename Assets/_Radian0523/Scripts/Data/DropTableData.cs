using System;
using System.Collections.Generic;
using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// 敵撃破時のドロップ抽選テーブル。
    /// ドロップ率と重み付きエントリでピックアップの出現確率を制御する。
    /// 新しいドロップパターンは Inspector でこの SO を1つ作るだけで追加でき、コード変更は不要。
    /// </summary>
    [CreateAssetMenu(fileName = "NewDropTable", menuName = "Velora/Drop Table Data")]
    public class DropTableData : ScriptableObject
    {
        [Range(0f, 1f)]
        [SerializeField] private float _dropRate = 0.3f;
        [SerializeField] private DropEntry[] _entries;

        public float DropRate => _dropRate;
        public IReadOnlyList<DropEntry> Entries => _entries;
    }

    [Serializable]
    public struct DropEntry
    {
        [SerializeField] private GameObject _pickupPrefab;
        [SerializeField] private int _weight;

        public GameObject PickupPrefab => _pickupPrefab;
        public int Weight => _weight;
    }
}
