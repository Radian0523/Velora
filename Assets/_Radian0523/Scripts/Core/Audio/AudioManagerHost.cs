using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// AudioManager の MonoBehaviour ホスト。
    /// AudioSource は MonoBehaviour が必要なため、この Host が保持・提供する。
    /// BGM は A/B 2つの AudioSource でクロスフェードを実現する。
    /// </summary>
    public class AudioManagerHost : MonoBehaviour
    {
        [SerializeField] private AudioSource _bgmSourceA;
        [SerializeField] private AudioSource _bgmSourceB;

        public AudioSource BgmSourceA => _bgmSourceA;
        public AudioSource BgmSourceB => _bgmSourceB;

        /// <summary>
        /// SE 再生用の AudioSource を生成する。
        /// AudioManager のコンストラクタから呼ばれ、SE 同時再生数分だけ事前生成する。
        /// </summary>
        public AudioSource CreateSESource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
