using Cysharp.Threading.Tasks;
using Velora.Wave;

namespace Velora.Core
{
    /// <summary>
    /// ウェーブ開始前の準備ステート。
    /// WaveEffectView の開始演出を await し、演出完了後に次ステートへ進む。
    /// </summary>
    public class BattleReadyState : GameStateBase
    {
        private readonly WaveEffectView _waveEffectView;
        private readonly WaveDirector _waveDirector;

        public BattleReadyState(WaveEffectView waveEffectView, WaveDirector waveDirector)
        {
            _waveEffectView = waveEffectView;
            _waveDirector = waveDirector;
        }

        public override async UniTask Enter()
        {
            await _waveEffectView.PlayWaveStartSequence(_waveDirector.CurrentWaveNumber);
        }
    }
}
