using Cysharp.Threading.Tasks;

namespace Velora.Core
{
    /// <summary>
    /// ゲームステートの基底クラス。
    /// GameFlowManager のステートマシンで使用する各ステートは
    /// このクラスを継承し、Enter/Update/Exit で自分の責務を完結させる。
    /// Enter/Exit を UniTask にすることで、演出の非同期シーケンスを
    /// async/await で直感的に記述できる。
    /// </summary>
    public abstract class GameStateBase
    {
        protected GameFlowManager Owner { get; private set; }

        public void SetOwner(GameFlowManager owner)
        {
            Owner = owner;
        }

        /// <summary>ステート開始時の処理。演出やUIの表示をここで行う。</summary>
        public virtual UniTask Enter() => UniTask.CompletedTask;

        /// <summary>毎フレームの更新処理。状態遷移の判定をここで行う。</summary>
        public virtual void Update() { }

        /// <summary>ステート終了時の処理。演出のクリーンアップをここで行う。</summary>
        public virtual UniTask Exit() => UniTask.CompletedTask;
    }
}
