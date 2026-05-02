using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Enemy
{
    /// <summary>
    /// スポーン演出待機ステート。
    /// タイマー経過後に Idle へ遷移する。演出は Prefab 未設定でもスキップされる。
    /// </summary>
    public class SpawnState : EnemyStateBase
    {
        private const float SpawnAnimationDuration = 0.5f;

        private float _timer;
        private GameObject _spawnEffect;

        public override UniTask Enter()
        {
            _timer = 0f;
            Controller.Agent.isStopped = true;
            Controller.PlayAnimation(EnemyController.AnimIdle);

            if (Controller.Data.SpawnEffectPrefab != null)
            {
                _spawnEffect = Object.Instantiate(
                    Controller.Data.SpawnEffectPrefab,
                    Controller.transform.position,
                    Quaternion.identity);
            }

            return UniTask.CompletedTask;
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= SpawnAnimationDuration)
            {
                StateMachine.ChangeState(EnemyState.Idle).Forget();
            }
        }

        public override UniTask Exit()
        {
            StopSpawnEffect();
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// ParticleSystem.Stop() で新規放出を停止し、既存パーティクルは寿命まで自然に消える。
        /// 全パーティクル消滅後に GameObject を自動破棄するため、手動の Destroy タイミング管理が不要。
        /// </summary>
        private void StopSpawnEffect()
        {
            if (_spawnEffect == null) return;

            var particles = _spawnEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            _spawnEffect = null;
        }
    }
}
