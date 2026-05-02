using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Velora.Enemy
{
    /// <summary>
    /// 死亡ステート。Collider 無効化 → 死亡アニメ → ディゾルブ演出 → プール返却を
    /// UniTask の非同期シーケンスで制御する。
    /// タイマー + Update ループではなく async/await で記述することで、
    /// フェーズの追加・タイミング調整がコード上で一覧できる。
    /// </summary>
    public class DeathState : EnemyStateBase
    {
        // 死亡アニメを見せる猶予。体が崩れ始める前に倒れるモーションを見せる。
        private const float PreDissolveDelay = 0.8f;
        private const float DissolveDuration = 1.5f;

        private CancellationTokenSource _cts;

        public override UniTask Enter()
        {
            Controller.Agent.isStopped = true;
            Controller.SetColliderEnabled(false);
            Controller.PlayAnimation(EnemyController.AnimDeath);

            if (Controller.Data.DeathEffectPrefab != null)
            {
                Object.Instantiate(
                    Controller.Data.DeathEffectPrefab,
                    Controller.transform.position,
                    Quaternion.identity);
            }

            _cts = new CancellationTokenSource();
            RunDeathSequence(_cts.Token).Forget();

            return UniTask.CompletedTask;
        }

        public override UniTask Exit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 死亡演出シーケンス。
        /// Phase 1: 死亡アニメの猶予（倒れるモーションを見せる）
        /// Phase 2: ディゾルブで体を消失させる
        /// Phase 3: プール返却
        /// </summary>
        private async UniTaskVoid RunDeathSequence(CancellationToken cancellationToken)
        {
            await UniTask.Delay(
                (int)(PreDissolveDelay * 1000),
                cancellationToken: cancellationToken);

            var dissolve = Controller.DissolveController;
            if (dissolve != null)
            {
                await dissolve.PlayDissolve(DissolveDuration, cancellationToken);
            }
            else
            {
                // DissolveController 未設定時のフォールバック（従来と同等の待機）
                await UniTask.Delay(
                    (int)(DissolveDuration * 1000),
                    cancellationToken: cancellationToken);
            }

            Controller.ReturnToPool();
        }
    }
}
