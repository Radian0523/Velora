using Cysharp.Threading.Tasks;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// ゲームオーバーステート。
    /// ResultPresenter.Show(isGameOver: true) を呼び出してリザルト画面を表示する。
    /// </summary>
    public class GameOverState : GameStateBase
    {
        private readonly ResultPresenter _resultPresenter;

        public GameOverState(ResultPresenter resultPresenter)
        {
            _resultPresenter = resultPresenter;
        }

        public override async UniTask Enter()
        {
            await _resultPresenter.Show(isGameOver: true);
        }
    }
}
