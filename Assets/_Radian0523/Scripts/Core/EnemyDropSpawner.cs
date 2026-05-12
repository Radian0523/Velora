using System.Collections.Generic;
using UnityEngine;
using Velora.Data;

namespace Velora.Core
{
    /// <summary>
    /// 敵撃破時にピックアップをドロップするスポナー。
    /// EnemyDiedEvent を購読し、DropTableData の確率・重みに基づいて抽選を行う。
    /// 抽選ロジックをここに集約し、DropTableData はデータのみ保持する責務分離。
    /// Battle シーンに配置して使用する。
    /// </summary>
    public class EnemyDropSpawner : MonoBehaviour
    {
        private const float SpawnHeightOffset = -0.5f;

        private void OnEnable() => EventBus.Subscribe<EnemyDiedEvent>(HandleEnemyDied);
        private void OnDisable() => EventBus.Unsubscribe<EnemyDiedEvent>(HandleEnemyDied);

        private void HandleEnemyDied(EnemyDiedEvent e)
        {
            if (e.DropTable == null) return;
            if (Random.value > e.DropTable.DropRate) return;

            var prefab = SelectDrop(e.DropTable);
            if (prefab == null) return;

            Instantiate(prefab, e.Position + Vector3.up * SpawnHeightOffset, Quaternion.identity);
        }

        /// <summary>
        /// 重み付き抽選。各エントリの Weight を累積し、乱数で選択する。
        /// </summary>
        private static GameObject SelectDrop(DropTableData table)
        {
            IReadOnlyList<DropEntry> entries = table.Entries;
            if (entries == null || entries.Count == 0) return null;

            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                totalWeight += entries[i].Weight;
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                cumulative += entries[i].Weight;
                if (roll < cumulative) return entries[i].PickupPrefab;
            }

            return entries[entries.Count - 1].PickupPrefab;
        }
    }
}
