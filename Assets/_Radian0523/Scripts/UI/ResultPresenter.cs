using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// リザルト画面の Presenter。
    /// ScoreManager から統計データを収集して ResultData を構築し、
    /// ResultView に渡す。GameOverState と ResultState の両方から使用される。
    /// </summary>
    public class ResultPresenter : MonoBehaviour
    {
        [SerializeField] private ResultView _resultView;

        private ScoreManager _scoreManager;

        public void Initialize(ScoreManager scoreManager)
        {
            _scoreManager = scoreManager;
        }

        /// <summary>
        /// リザルト画面を表示する。
        /// isGameOver=true ならゲームオーバー、false なら全ウェーブクリアを示す。
        /// </summary>
        public async UniTask Show(bool isGameOver)
        {
            var data = new ResultData(
                _scoreManager.WavesReached,
                _scoreManager.TotalScore,
                _scoreManager.TotalKills,
                _scoreManager.SurvivalTime,
                isGameOver);

            await _resultView.ShowResult(data);
        }
    }
}
