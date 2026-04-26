using UnityEngine;

namespace Velora.Data
{
    public enum UpgradeType
    {
        DamageBoost,
        FireRateBoost,
        MaxHealthBoost,
        ReloadSpeedBoost,
        HealNow,
        NewWeapon
    }

    public enum UpgradeRarity
    {
        Common,
        Rare,
        Epic
    }

    /// <summary>
    /// アップグレードの定義データ。
    /// ウェーブクリア後にランダム提示されるカードの内容を決める。
    /// ScriptableObject 1つ作成 = 新アップグレード追加のデータドリブン設計。
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Velora/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string _upgradeName;
        [SerializeField][TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private UpgradeType _upgradeType;
        [SerializeField] private float _effectValue;
        [SerializeField] private UpgradeRarity _rarity;

        public string UpgradeName => _upgradeName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public UpgradeType UpgradeType => _upgradeType;
        public float EffectValue => _effectValue;
        public UpgradeRarity Rarity => _rarity;
    }
}
