using UnityEngine;
using Velora.Data;
using Velora.Enemy;

namespace Velora.Core
{
    /// <summary>
    /// Battle シーンの設定値をまとめた値オブジェクト。
    /// VContainer の DI コンテナでは型で区別するため、
    /// 同じ型（Transform, WaveData[] 等）の複数パラメータを個別に登録できない。
    /// この Config に束ねることで、型の衝突を回避しつつ意味のある単位でグルーピングする。
    /// </summary>
    public class BattleConfig
    {
        public WaveData[] WaveDataList { get; }
        public EnemyController EnemyPrefab { get; }
        public Transform PoolParent { get; }
        public BattleSoundData BattleSoundData { get; }

        public BattleConfig(
            WaveData[] waveDataList,
            EnemyController enemyPrefab,
            Transform poolParent,
            BattleSoundData battleSoundData)
        {
            WaveDataList = waveDataList;
            EnemyPrefab = enemyPrefab;
            PoolParent = poolParent;
            BattleSoundData = battleSoundData;
        }
    }
}
