using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Velora.Data
{
    [Serializable]
    public class SpawnEntry
    {
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private int _count = 1;
        [SerializeField] private float _spawnDelay = 0.3f;

        public EnemyData EnemyData => _enemyData;
        public int Count => _count;
        public float SpawnDelay => _spawnDelay;
    }

    /// <summary>
    /// 1ウェーブ分のスポーン構成を定義する ScriptableObject。
    /// SpawnEntry を追加するだけで敵の出現パターンを調整できるデータドリブン設計。
    /// </summary>
    [CreateAssetMenu(fileName = "NewWave", menuName = "Velora/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private int _waveNumber;
        [SerializeField] private List<SpawnEntry> _spawnEntries = new();

        public int WaveNumber => _waveNumber;
        public IReadOnlyList<SpawnEntry> SpawnEntries => _spawnEntries;
        public int TotalEnemyCount => _spawnEntries.Sum(e => e.Count);
    }
}
