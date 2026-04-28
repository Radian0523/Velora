using Cysharp.Threading.Tasks;
using Velora.UI;

namespace Velora.Core
{
    /// <summary>
    /// 全ウェーブクリア時のリザルトステート。
    /// ResultPresenter.Show(isGameOver: false) を呼び出してクリアリザルトを表示する。
    /// GameOverState と同じ Presenter を使い、isGameOver フラグで表示内容を切り替える。
    /// </summary>
    public class ResultState : GameStateBase
    {
        private readonly ResultPresenter _resultPresenter;

        public ResultState(ResultPresenter resultPresenter)
        {
            _resultPresenter = resultPresenter;
        }

        public override async UniTask Enter()
        {
            await _resultPresenter.Show(isGameOver: false);
        }
    }
}
