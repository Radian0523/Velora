using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Velora.Wave
{
    /// <summary>
    /// スポーン地点を管理する MonoBehaviour。
    /// プレイヤーから一定距離以上離れたポイントを優先選択することで、
    /// 目の前に突然出現する不自然さを回避する。
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private float _minSpawnDistance = 15f;

        private const float NavMeshSampleRadius = 5f;

        // 候補インデックスのキャッシュ（毎フレーム呼ばれないため GC 許容）
        private readonly List<int> _candidates = new();

        public int SpawnPointCount => _spawnPoints.Length;

        /// <summary>
        /// プレイヤー位置から _minSpawnDistance 以上離れたポイントをランダムに返す。
        /// 全ポイントが近い場合は最も遠いポイントをフォールバックとして使用する。
        /// 最終的に NavMesh 上にスナップし、到達不能な場所へのスポーンを防ぐ。
        /// </summary>
        public Vector3 GetSpawnPosition(Vector3 playerPosition)
        {
            _candidates.Clear();
            int farthestIndex = 0;
            float farthestDistance = 0f;

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                float distance = Vector3.Distance(_spawnPoints[i].position, playerPosition);

                if (distance >= _minSpawnDistance)
                {
                    _candidates.Add(i);
                }
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndex = i;
                }
            }

            // 条件を満たす候補からランダム選択。なければ最遠をフォールバック
            int selectedIndex = _candidates.Count > 0
                ? _candidates[Random.Range(0, _candidates.Count)]
                : farthestIndex;

            Vector3 selectedPosition = _spawnPoints[selectedIndex].position;

            // NavMesh 上にスナップして到達不能な位置へのスポーンを防止
            if (NavMesh.SamplePosition(selectedPosition, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return selectedPosition;
        }
    }
}
