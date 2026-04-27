using System.Threading;
using Cysharp.Threading.Tasks;
using Velora.Wave;

namespace Velora.Core
{
    /// <summary>
    /// 戦闘中ステート。WaveDirector の StartWave を呼び出して敵のスポーンを開始する。
    /// スポーン完了後もステートは継続し、全滅判定は WaveDirector のイベントで行う。
    /// </summary>
    public class BattleInProgressState : GameStateBase
    {
        private readonly WaveDirector _waveDirector;
        private CancellationTokenSource _cts;

        public BattleInProgressState(WaveDirector waveDirector)
        {
            _waveDirector = waveDirector;
        }

        public override async UniTask Enter()
        {
            _cts = new CancellationTokenSource();
            await _waveDirector.StartWave(_cts.Token);
        }

        public override UniTask Exit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            return UniTask.CompletedTask;
        }
    }
}
