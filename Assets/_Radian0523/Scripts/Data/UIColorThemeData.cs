using UnityEngine;
using Velora.Weapon;

namespace Velora.Data
{
    /// <summary>
    /// UI 全体のカラー定義を一元管理する ScriptableObject。
    /// 各 View/Presenter にハードコードされていた色値をここに集約することで、
    /// テーマ変更や色調整時にこのアセット1つを編集するだけで済む。
    /// </summary>
    [CreateAssetMenu(fileName = "UIColorTheme", menuName = "Velora/UI Color Theme")]
    public class UIColorThemeData : ScriptableObject
    {
        [Header("弾薬タイプ")]
        [SerializeField] private Color _lightAmmoColor = new Color(1f, 0.843f, 0f);
        [SerializeField] private Color _energyAmmoColor = new Color(0f, 0.898f, 1f);
        [SerializeField] private Color _explosiveAmmoColor = new Color(1f, 0.188f, 0.188f);

        [Header("ダメージ表示")]
        [SerializeField] private Color _normalHitColor = Color.white;
        [SerializeField] private Color _headshotColor = Color.red;

        [Header("HPバー")]
        [SerializeField] private Color _healthyColor = new Color(0.2f, 0.85f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(0.95f, 0.85f, 0.1f);
        [SerializeField] private Color _criticalColor = new Color(0.9f, 0.15f, 0.15f);
        [SerializeField] private float _warningThreshold = 0.5f;
        [SerializeField] private float _criticalThreshold = 0.25f;

        [Header("被弾演出")]
        [SerializeField] private Color _damageDirectionColor = new Color(0.8f, 0f, 0f, 0.9f);
        [SerializeField] private Color _damageVignetteColor = new Color(0.88f, 0.22f, 0.22f);

        public Color NormalHitColor => _normalHitColor;
        public Color HeadshotColor => _headshotColor;
        public Color HealthyColor => _healthyColor;
        public Color WarningColor => _warningColor;
        public Color CriticalColor => _criticalColor;
        public float WarningThreshold => _warningThreshold;
        public float CriticalThreshold => _criticalThreshold;
        public Color DamageDirectionColor => _damageDirectionColor;
        public Color DamageVignetteColor => _damageVignetteColor;

        public Color GetAmmoTypeColor(AmmoType ammoType)
        {
            return ammoType switch
            {
                AmmoType.Light => _lightAmmoColor,
                AmmoType.Energy => _energyAmmoColor,
                AmmoType.Explosive => _explosiveAmmoColor,
                _ => Color.white
            };
        }
    }
}
