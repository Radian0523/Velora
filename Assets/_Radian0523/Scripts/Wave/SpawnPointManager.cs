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

        private const float MinSpawnDistance = 15f;
        private const float NavMeshSampleRadius = 5f;

        public int SpawnPointCount => _spawnPoints.Length;

        /// <summary>
        /// プレイヤー位置から MinSpawnDistance 以上離れたポイントをランダムに返す。
        /// 全ポイントが近い場合は最も遠いポイントをフォールバックとして使用する。
        /// 最終的に NavMesh 上にスナップし、到達不能な場所へのスポーンを防ぐ。
        /// </summary>
        public Vector3 GetSpawnPosition(Vector3 playerPosition)
        {
            int farthestIndex = 0;
            float farthestDistance = 0f;

            // MinSpawnDistance 以上のポイントを数えつつ、最遠も記録する
            int candidateCount = 0;
            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                float distance = Vector3.Distance(_spawnPoints[i].position, playerPosition);
                if (distance >= MinSpawnDistance)
                {
                    candidateCount++;
                }
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndex = i;
                }
            }

            Vector3 selectedPosition;

            if (candidateCount > 0)
            {
                // 条件を満たすポイントからランダム選択
                int randomTarget = Random.Range(0, candidateCount);
                int current = 0;
                selectedPosition = _spawnPoints[farthestIndex].position;

                for (int i = 0; i < _spawnPoints.Length; i++)
                {
                    float distance = Vector3.Distance(_spawnPoints[i].position, playerPosition);
                    if (distance >= MinSpawnDistance)
                    {
                        if (current == randomTarget)
                        {
                            selectedPosition = _spawnPoints[i].position;
                            break;
                        }
                        current++;
                    }
                }
            }
            else
            {
                selectedPosition = _spawnPoints[farthestIndex].position;
            }

            // NavMesh 上にスナップして到達不能な位置へのスポーンを防止
            if (NavMesh.SamplePosition(selectedPosition, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return selectedPosition;
        }
    }
}
