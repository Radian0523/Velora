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

        public AudioClip PlayerDamageSound => _playerDamageSound;
        public AudioClip PlayerDeathSound => _playerDeathSound;
        public AudioClip WaveClearSound => _waveClearSound;
    }
}
