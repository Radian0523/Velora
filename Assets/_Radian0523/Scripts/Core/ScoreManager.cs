using System;

namespace Velora.Core
{
    /// <summary>
    /// バトル中のスコアとプレイ統計を集計する pure C# クラス。
    /// EventBus を購読することで、WeaponController・WaveDirector・EnemyController に
    /// 直接依存せずに統計を収集できる（疎結合）。
    /// </summary>
    public class ScoreManager : IDisposable
    {
        private int _totalKills;
        private int _totalScore;
        private int _totalShots;
        private int _hitCount;
        private int _wavesReached;
        private float _survivalTime;
        private bool _isDisposed;

        public int TotalKills => _totalKills;
        public int TotalScore => _totalScore;
        public int TotalShots => _totalShots;
        public int HitCount => _hitCount;
        public int WavesReached => _wavesReached;
        public float SurvivalTime => _survivalTime;

        /// <summary>命中率 0〜100 (%)。0発撃った場合は 0% を返す。</summary>
        public float Accuracy => _totalShots > 0 ? (float)_hitCount / _totalShots * 100f : 0f;

        public ScoreManager()
        {
            EventBus.Subscribe<EnemyDiedEvent>(HandleEnemyDied);
            EventBus.Subscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
            EventBus.Subscribe<WeaponFiredEvent>(HandleWeaponFired);
            EventBus.Subscribe<WaveStartedEvent>(HandleWaveStarted);
        }

        /// <summary>
        /// 生存時間を加算する。BattleSceneDirector.Update() から毎フレーム呼ばれる。
        /// ゲームオーバーや結果画面に遷移後も呼ばれ続けるが、
        /// リザルト画面での表示は Show() 時点の値を使うため実害はない。
        /// </summary>
        public void UpdateSurvivalTime(float deltaTime)
        {
            _survivalTime += deltaTime;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            EventBus.Unsubscribe<EnemyDiedEvent>(HandleEnemyDied);
            EventBus.Unsubscribe<EnemyDamagedEvent>(HandleEnemyDamaged);
            EventBus.Unsubscribe<WeaponFiredEvent>(HandleWeaponFired);
            EventBus.Unsubscribe<WaveStartedEvent>(HandleWaveStarted);
        }

        private void HandleEnemyDied(EnemyDiedEvent e)
        {
            _totalKills++;
            _totalScore += e.ScoreValue;
        }

        private void HandleEnemyDamaged(EnemyDamagedEvent e)
        {
            // ヒット判定は EnemyDamagedEvent の発火回数で代替する。
            // 1発で複数の EnemyDamagedEvent が発火する武器（散弾等）を追加する場合は
            // WeaponFiredEvent に HitCount フィールドを追加して再設計する。
            _hitCount++;
        }

        private void HandleWeaponFired(WeaponFiredEvent e)
        {
            _totalShots++;
        }

        private void HandleWaveStarted(WaveStartedEvent e)
        {
            // WaveNumber は 1 始まりなので到達ウェーブ数として使える
            _wavesReached = e.WaveNumber;
        }
    }
}
