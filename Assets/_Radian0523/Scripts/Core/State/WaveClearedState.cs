using Cysharp.Threading.Tasks;
using Velora.Wave;

namespace Velora.Core
{
    /// <summary>
    /// ウェーブクリア演出ステート。
    /// WaveEffectView のクリア演出を await し、演出完了後に次ステートへ進む。
    /// </summary>
    public class WaveClearedState : GameStateBase
    {
        private readonly WaveEffectView _waveEffectView;
        private readonly WaveDirector _waveDirector;

        public WaveClearedState(WaveEffectView waveEffectView, WaveDirector waveDirector)
        {
            _waveEffectView = waveEffectView;
            _waveDirector = waveDirector;
        }

        public override async UniTask Enter()
        {
            await _waveEffectView.PlayWaveClearSequence(_waveDirector.CurrentWaveNumber);
        }
    }
}
