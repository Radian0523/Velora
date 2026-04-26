using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// 死亡ステート。Collider 無効化、エフェクト再生後にプール返却する。
    /// </summary>
    public class DeathState : EnemyStateBase
    {
        private const float DeathSequenceDuration = 1f;

        private float _timer;
        private bool _isReturned;

        public override UniTask Enter()
        {
            _timer = 0f;
            _isReturned = false;
            Controller.Agent.isStopped = true;
            Controller.SetColliderEnabled(false);

            if (Controller.Data.DeathEffectPrefab != null)
            {
                Object.Instantiate(
                    Controller.Data.DeathEffectPrefab,
                    Controller.transform.position,
                    Quaternion.identity);
            }

            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            if (_isReturned) return;

            _timer += Time.deltaTime;
            if (_timer >= DeathSequenceDuration)
            {
                _isReturned = true;
                Controller.ReturnToPool();
            }
        }
    }
}
