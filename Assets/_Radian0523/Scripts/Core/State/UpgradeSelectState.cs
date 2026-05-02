using Cysharp.Threading.Tasks;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// アップグレード選択ステート。
    /// UpgradeSelectPresenter.ShowAndWait() を await することで、
    /// プレイヤーがカードを選ぶまでゲームフローをここで一時停止する。
    /// UniTaskCompletionSource によるシグナル待機は、
    /// ポーリングやコルーチン管理より意図を明示的に表現できる。
    /// </summary>
    public class UpgradeSelectState : GameStateBase
    {
        private readonly UpgradeSelectPresenter _presenter;

        public UpgradeSelectState(UpgradeSelectPresenter presenter)
        {
            _presenter = presenter;
        }

        public override async UniTask Enter()
        {
            await _presenter.ShowAndWait();
        }
    }
}
