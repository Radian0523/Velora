using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// BGM / SE の再生を管理する pure C# クラス。
    /// BGM は A/B 2つの AudioSource を切り替えてクロスフェードする。
    /// SE は事前生成した AudioSource 配列から空いているものを使い回す。
    /// ボリューム設定は PlayerPrefs に永続化する。
    ///
    /// クロスフェードは UniTask の手動補間ループで実装している。
    /// DOTween の DOFade + AsyncWaitForCompletion はヒープアロケーションが多く、
    /// BGM 切り替え時に GC スパイクでフレーム落ちを起こすため採用しなかった。
    /// </summary>
    public class AudioManager : IDisposable
    {
        private const int MaxConcurrentSE = 16;
        private const string BgmVolumeKey = "BGMVolume";
        private const string SeVolumeKey = "SEVolume";
        private const float DefaultVolume = 1f;
        private const float DefaultFadeDuration = 0.5f;

        private readonly AudioSource _bgmSourceA;
        private readonly AudioSource _bgmSourceB;
        private readonly AudioSource[] _seSources;

        private AudioSource _currentBgmSource;
        private CancellationTokenSource _bgmCts;
        private float _bgmVolume;
        private float _seVolume;
        private bool _isDisposed;

        public float BgmVolume => _bgmVolume;
        public float SeVolume => _seVolume;

        public AudioManager(AudioManagerHost host)
        {
            _bgmSourceA = host.BgmSourceA;
            _bgmSourceB = host.BgmSourceB;
            _currentBgmSource = _bgmSourceA;

            _seSources = new AudioSource[MaxConcurrentSE];
            for (int i = 0; i < MaxConcurrentSE; i++)
            {
                _seSources[i] = host.CreateSESource();
            }

            LoadVolumeSettings();
        }

        /// <summary>
        /// BGM をクロスフェードで切り替える。
        /// 現在の BGM をフェードアウトしながら新しい BGM をフェードインすることで、
        /// シーン遷移時の途切れを防ぐ。
        /// </summary>
        public async UniTask PlayBGM(AudioClip clip, float fadeDuration = DefaultFadeDuration)
        {
            if (clip == null) return;

            CancelBgmFade();
            _bgmCts = new CancellationTokenSource();
            var ct = _bgmCts.Token;

            // オーディオデータの非同期プリロード。
            // Play() 時の同期デコードによるフレーム落ちを防ぐ。
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                await UniTask.WaitUntil(
                    () => clip.loadState == AudioDataLoadState.Loaded,
                    cancellationToken: ct);
            }

            var prevSource = _currentBgmSource;
            var nextSource = (prevSource == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;
            float prevStartVolume = prevSource.volume;

            nextSource.clip = clip;
            nextSource.volume = 0f;
            nextSource.loop = true;
            nextSource.Play();
            _currentBgmSource = nextSource;

            // Play() 直後はオーディオスレッドがバッファを充填するため負荷が高い。
            // 1フレーム空けることでそのスパイクとフェード処理が同一フレームに重なるのを防ぐ。
            await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, ct);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                prevSource.volume = Mathf.Lerp(prevStartVolume, 0f, t);
                nextSource.volume = Mathf.Lerp(0f, _bgmVolume, t);
                await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, ct);
            }

            prevSource.volume = 0f;
            prevSource.Stop();
            nextSource.volume = _bgmVolume;
        }

        public async UniTask StopBGM(float fadeDuration = DefaultFadeDuration)
        {
            CancelBgmFade();
            _bgmCts = new CancellationTokenSource();
            var ct = _bgmCts.Token;

            float startVolume = _currentBgmSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                _currentBgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                await UniTask.Yield(PlayerLoopTiming.PreLateUpdate, ct);
            }

            _currentBgmSource.volume = 0f;
            _currentBgmSource.Stop();
        }

        /// <summary>
        /// SE を再生する。空いている AudioSource を探して再生する。
        /// 全チャネルが使用中の場合は最も古い SE を上書きする。
        /// </summary>
        public void PlaySE(AudioClip clip)
        {
            if (clip == null) return;

            var source = FindAvailableSESource();
            source.volume = _seVolume;
            source.PlayOneShot(clip);
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            _currentBgmSource.volume = _bgmVolume;
            PlayerPrefs.SetFloat(BgmVolumeKey, _bgmVolume);
        }

        public void SetSEVolume(float volume)
        {
            _seVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SeVolumeKey, _seVolume);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            CancelBgmFade();
        }

        private void CancelBgmFade()
        {
            _bgmCts?.Cancel();
            _bgmCts?.Dispose();
            _bgmCts = null;
        }

        private void LoadVolumeSettings()
        {
            _bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
            _seVolume = PlayerPrefs.GetFloat(SeVolumeKey, DefaultVolume);

            _bgmSourceA.volume = 0f;
            _bgmSourceB.volume = 0f;
        }

        private AudioSource FindAvailableSESource()
        {
            // 再生中でない AudioSource を優先的に使用する
            foreach (var source in _seSources)
            {
                if (!source.isPlaying) return source;
            }

            // 全チャネルが使用中の場合は先頭を上書き
            return _seSources[0];
        }
    }
}
