using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// リザルト画面の Presenter。
    /// ScoreManager から統計データを収集して ResultData を構築し、
    /// ResultView に渡す。GameOverState と ResultState の両方から使用される。
    /// Retry / Title ボタンのナビゲーション処理を担当する。
    /// </summary>
    public class ResultPresenter : MonoBehaviour
    {
        [SerializeField] private ResultView _resultView;

        private ScoreManager _scoreManager;
        private SceneNavigator _sceneNavigator;

        [Inject]
        public void Construct(SceneNavigator sceneNavigator)
        {
            _sceneNavigator = sceneNavigator;
        }

        public void Initialize(ScoreManager scoreManager)
        {
            _scoreManager = scoreManager;
            _resultView.OnRetryClicked += HandleRetry;
            _resultView.OnTitleClicked += HandleTitle;
        }

        private void OnDestroy()
        {
            if (_resultView != null)
            {
                _resultView.OnRetryClicked -= HandleRetry;
                _resultView.OnTitleClicked -= HandleTitle;
            }
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

        private void HandleRetry()
        {
            TransitionToRetry().Forget();
        }

        private void HandleTitle()
        {
            TransitionToTitle().Forget();
        }

        private async UniTaskVoid TransitionToRetry()
        {
            await _sceneNavigator.TransitionTo("Battle", "Battle");
        }

        private async UniTaskVoid TransitionToTitle()
        {
            await _sceneNavigator.TransitionTo("Title", "Battle");
        }
    }
}
