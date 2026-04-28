using System.Collections.Generic;
using UnityEngine;
using Velora.Data;
using Velora.Player;

namespace Velora.Upgrade
{
    /// <summary>
    /// アップグレードの抽選と適用を担当する pure C# クラス。
    /// レアリティ重み付きでランダムに 3 択を提示する。
    /// ScriptableObject のリストを渡すだけで新アップグレードが追加できる設計。
    /// </summary>
    public class UpgradeManager
    {
        private readonly IReadOnlyList<UpgradeData> _allUpgrades;

        private const int ChoiceCount = 3;

        // レアリティごとの出現重み。値が大きいほど選ばれやすい。
        // 合計100 になるように設定することで直感的に確率を調整できる。
        private static readonly Dictionary<UpgradeRarity, int> RarityWeights = new()
        {
            { UpgradeRarity.Common, 60 },
            { UpgradeRarity.Rare,   30 },
            { UpgradeRarity.Epic,   10 },
        };

        public UpgradeManager(IReadOnlyList<UpgradeData> allUpgrades)
        {
            _allUpgrades = allUpgrades;
        }

        /// <summary>
        /// レアリティ重み付きで重複なし 3 択を返す。
        /// 登録アップグレード数が ChoiceCount 未満の場合は全件返す。
        /// </summary>
        public List<UpgradeData> GetRandomChoices()
        {
            // 重み付きプールを構築する。
            // 同一 SO が重複して入るため、Remove で重複排除するより
            // 別リストで選択済みを管理する方が安全。
            var weightedPool = BuildWeightedPool();
            var choices = new List<UpgradeData>(ChoiceCount);
            int maxAttempts = weightedPool.Count * 3;

            for (int attempt = 0; attempt < maxAttempts && choices.Count < ChoiceCount; attempt++)
            {
                var candidate = weightedPool[Random.Range(0, weightedPool.Count)];

                if (!choices.Contains(candidate))
                {
                    choices.Add(candidate);
                }
            }

            return choices;
        }

        /// <summary>
        /// 選択されたアップグレードを PlayerModel に適用する。
        /// 適用ロジックは PlayerModel に集約し、UpgradeManager は仲介のみ行う。
        /// </summary>
        public void ApplyUpgrade(UpgradeData upgrade, PlayerModel playerModel)
        {
            playerModel.ApplyUpgrade(upgrade);
        }

        private List<UpgradeData> BuildWeightedPool()
        {
            var pool = new List<UpgradeData>();

            foreach (var upgrade in _allUpgrades)
            {
                if (upgrade == null) continue;

                int weight = RarityWeights.TryGetValue(upgrade.Rarity, out int w) ? w : 1;
                for (int i = 0; i < weight; i++)
                {
                    pool.Add(upgrade);
                }
            }

            return pool;
        }
    }
}
