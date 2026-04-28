namespace Velora.UI
{
    /// <summary>
    /// リザルト画面に表示するプレイ統計データ。
    /// ScoreManager から収集した値を ResultPresenter がここにまとめて ResultView に渡す。
    /// readonly struct にすることで不変性を保証し、GC Alloc を回避する。
    /// </summary>
    public readonly struct ResultData
    {
        public int WavesReached { get; }
        public int TotalScore { get; }
        public int TotalKills { get; }
        public float SurvivalTime { get; }
        public bool IsGameOver { get; }

        public ResultData(int wavesReached, int totalScore, int totalKills, float survivalTime, bool isGameOver)
        {
            WavesReached = wavesReached;
            TotalScore = totalScore;
            TotalKills = totalKills;
            SurvivalTime = survivalTime;
            IsGameOver = isGameOver;
        }
    }
}
