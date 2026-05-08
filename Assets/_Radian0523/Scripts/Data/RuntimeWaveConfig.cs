using System.Collections.Generic;
using System.Linq;

namespace Velora.Data
{
    /// <summary>
    /// WaveData.SpawnEntry のランタイム版。
    /// SpawnEntry は SerializeField の private setter で変更不可のため、
    /// AI Director が自由に構築できるミュータブルなクラスを用意する。
    /// </summary>
    public class RuntimeSpawnEntry
    {
        public EnemyData EnemyData { get; }
        public int Count { get; }
        public float SpawnDelay { get; }

        public RuntimeSpawnEntry(EnemyData enemyData, int count, float spawnDelay)
        {
            EnemyData = enemyData;
            Count = count;
            SpawnDelay = spawnDelay;
        }
    }

    /// <summary>
    /// WaveData（ScriptableObject）のランタイム版。
    /// AI Director がパフォーマンス評価に基づいて動的に構築する。
    /// base wave の修正版（Wave 1〜3）とエンドレス生成（Wave 4+）の両方に対応する。
    /// </summary>
    public class RuntimeWaveConfig
    {
        public int WaveNumber { get; }
        public IReadOnlyList<RuntimeSpawnEntry> SpawnEntries { get; }
        public float HealthMultiplier { get; }
        public int TotalEnemyCount { get; }

        public RuntimeWaveConfig(
            int waveNumber,
            IReadOnlyList<RuntimeSpawnEntry> spawnEntries,
            float healthMultiplier)
        {
            WaveNumber = waveNumber;
            SpawnEntries = spawnEntries;
            HealthMultiplier = healthMultiplier;
            TotalEnemyCount = spawnEntries.Sum(e => e.Count);
        }

        /// <summary>
        /// WaveData から RuntimeWaveConfig に変換するファクトリ。
        /// base wave をそのまま使う場合や、AI Director の修正前ベースに利用する。
        /// </summary>
        public static RuntimeWaveConfig FromWaveData(WaveData waveData, float healthMultiplier = 1f)
        {
            var entries = new List<RuntimeSpawnEntry>();
            foreach (var entry in waveData.SpawnEntries)
            {
                entries.Add(new RuntimeSpawnEntry(entry.EnemyData, entry.Count, entry.SpawnDelay));
            }

            return new RuntimeWaveConfig(waveData.WaveNumber, entries, healthMultiplier);
        }
    }
}
