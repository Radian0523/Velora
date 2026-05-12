using System.Collections.Generic;
using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// バトルシーン全体で共有するグローバル効果音の ScriptableObject。
    /// プレイヤー被弾・死亡・ウェーブクリアなど、特定エンティティに属さない音声を管理する。
    /// エンティティ固有の音声は各 Data SO（WeaponData, EnemyData）に配置する。
    /// </summary>
    [CreateAssetMenu(fileName = "BattleSoundData", menuName = "Velora/Battle Sound Data")]
    public class BattleSoundData : ScriptableObject
    {
        [Header("プレイヤー")]
        [SerializeField] private AudioClip _playerDamageSound;
        [SerializeField] private AudioClip _playerDeathSound;

        [Header("ウェーブ")]
        [SerializeField] private AudioClip _waveClearSound;

        [Header("Ammo")]
        [SerializeField] private AudioClip _ammoPickupSound;

        [Header("Health")]
        [SerializeField] private AudioClip _healthPickupSound;

        [Header("BGM")]
        [SerializeField] private AudioClip[] _bgmClips;

        public AudioClip PlayerDamageSound => _playerDamageSound;
        public AudioClip PlayerDeathSound => _playerDeathSound;
        public AudioClip WaveClearSound => _waveClearSound;
        public AudioClip AmmoPickupSound => _ammoPickupSound;
        public AudioClip HealthPickupSound => _healthPickupSound;
        public IReadOnlyList<AudioClip> BgmClips => _bgmClips;
    }
}
