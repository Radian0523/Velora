using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// BGM / SE の再生を管理する pure C# クラス。
    /// BGM は A/B 2つの AudioSource を切り替えてクロスフェードする。
    /// SE は事前生成した AudioSource 配列から空いているものを使い回す。
    /// ボリューム設定は PlayerPrefs に永続化する。
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

            var nextSource = (_currentBgmSource == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;
            nextSource.clip = clip;
            nextSource.volume = 0f;
            nextSource.loop = true;
            nextSource.Play();

            // 現在の BGM フェードアウトと新 BGM フェードインを同時実行
            var fadeOutTween = _currentBgmSource.DOFade(0f, fadeDuration).SetUpdate(true);
            var fadeInTween = nextSource.DOFade(_bgmVolume, fadeDuration).SetUpdate(true);

            await UniTask.WhenAll(
                fadeOutTween.AsyncWaitForCompletion().AsUniTask(),
                fadeInTween.AsyncWaitForCompletion().AsUniTask());

            _currentBgmSource.Stop();
            _currentBgmSource = nextSource;
        }

        public async UniTask StopBGM(float fadeDuration = DefaultFadeDuration)
        {
            await _currentBgmSource.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .AsyncWaitForCompletion();
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

            _bgmSourceA.DOKill();
            _bgmSourceB.DOKill();
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
