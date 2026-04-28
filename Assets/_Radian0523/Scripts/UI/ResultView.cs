using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Velora.UI
{
    /// <summary>
    /// リザルト画面の View 層。
    /// スコア・キル数・生存時間を DOTween でカウントアップ表示して
    /// プレイヤーに達成感を与える演出を実現する。
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [Header("テキスト")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _killText;
        [SerializeField] private TextMeshProUGUI _survivalTimeText;

        [Header("ボタン")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        [Header("アニメーション")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _countUpDuration = 1.5f;
        [SerializeField] private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _retryButton.onClick.AddListener(HandleRetry);
            _titleButton.onClick.AddListener(HandleTitle);
        }

        /// <summary>
        /// リザルト画面を表示し、各統計値をカウントアップアニメーションで見せる。
        /// AsyncWaitForCompletion() で演出終了を待機することで、
        /// State 側が「表示完了」を検知できる。
        /// </summary>
        public async UniTask ShowResult(ResultData data)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _retryButton.interactable = false;
            _titleButton.interactable = false;

            _titleText.text = data.IsGameOver ? "GAME OVER" : "CLEAR";

            await _canvasGroup.DOFade(1f, _fadeInDuration).AsyncWaitForCompletion();

            // 4項目を同時カウントアップする。
            // ローカル float 変数を DOTween の getter にすることで、
            // 外部状態を汚染せずにアニメーション値を管理できる。
            // 各 DOTween.To は呼び出し時点で即座に開始するため、
            // Task.WhenAll で全ての完了を待機できる。
            float v1 = 0f, v2 = 0f, v3 = 0f, v4 = 0f;

            var t1 = DOTween.To(() => v1, v => { v1 = v; _waveText.text         = $"WAVE: {Mathf.RoundToInt(v)}";  }, (float)data.WavesReached, _countUpDuration);
            var t2 = DOTween.To(() => v2, v => { v2 = v; _scoreText.text        = $"SCORE: {Mathf.RoundToInt(v)}"; }, (float)data.TotalScore,    _countUpDuration);
            var t3 = DOTween.To(() => v3, v => { v3 = v; _killText.text         = $"KILLS: {Mathf.RoundToInt(v)}"; }, (float)data.TotalKills,    _countUpDuration);
            var t4 = DOTween.To(() => v4, v => { v4 = v; _survivalTimeText.text = $"TIME: {Mathf.FloorToInt(v)}s"; }, data.SurvivalTime,         _countUpDuration);

            await Task.WhenAll(
                t1.AsyncWaitForCompletion(),
                t2.AsyncWaitForCompletion(),
                t3.AsyncWaitForCompletion(),
                t4.AsyncWaitForCompletion());

            _retryButton.interactable = true;
            _titleButton.interactable = true;
        }

        private void HandleRetry()
        {
            // シーン全体をリロードすることでゲームの完全リセットを保証する。
            // ゲームループをコードでリセットするよりシンプルで確実な方法。
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleTitle()
        {
            // Phase 6 でタイトルシーン遷移を実装予定
        }
    }
}
