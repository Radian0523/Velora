using UnityEngine;

namespace Velora.Data
{
    public enum WeaponType
    {
        Hitscan,
        Projectile
    }

    /// <summary>
    /// 武器パラメータの ScriptableObject。
    /// 新しい武器は Inspector でこの SO を1つ作るだけで追加できる。
    /// ロジック側は WeaponType で射撃方式を自動選択するため、コード変更は不要。
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Velora/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("基本情報")]
        [SerializeField] private string _weaponName;
        [SerializeField] private WeaponType _weaponType;

        [Header("モデル")]
        [SerializeField] private GameObject _modelPrefab;

        [Header("戦闘パラメータ")]
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _fireRate = 10f;
        [SerializeField] private int _maxAmmo = 30;
        [SerializeField] private float _reloadTime = 1.5f;
        [SerializeField] private float _headshotMultiplier = 2f;

        [Header("拡散・反動")]
        [SerializeField] private float _spreadAngle = 1f;
        [SerializeField] private float _adsSpreadAngle = 0.2f;
        [SerializeField] private RecoilData _recoilPattern;

        [Header("ADS")]
        [SerializeField] private float _adsFieldOfView = 45f;

        [Header("Projectile 専用")]
        [SerializeField] private float _projectileSpeed = 50f;
        [SerializeField] private float _projectileMaxLifetime = 5f;
        [SerializeField] private GameObject _projectilePrefab;

        [Header("UI")]
        [SerializeField] private Sprite _icon;

        [Header("エフェクト")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;

        [Header("武器キック（DOTween Punch で銃のリアクション演出）")]
        [SerializeField] private float _kickBackDistance = 0.05f;
        [SerializeField] private float _kickUpAngle = 3f;
        [SerializeField] private float _kickDuration = 0.1f;
        [SerializeField] private int _kickVibrato = 1;

        [Header("サウンド")]
        [SerializeField] private AudioClip _fireSound;

        public string WeaponName => _weaponName;
        public WeaponType WeaponType => _weaponType;
        public GameObject ModelPrefab => _modelPrefab;
        public float Damage => _damage;
        public float FireRate => _fireRate;
        public int MaxAmmo => _maxAmmo;
        public float ReloadTime => _reloadTime;
        public float HeadshotMultiplier => _headshotMultiplier;
        public float SpreadAngle => _spreadAngle;
        public float AdsSpreadAngle => _adsSpreadAngle;
        public RecoilData RecoilPattern => _recoilPattern;
        public float AdsFieldOfView => _adsFieldOfView;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileMaxLifetime => _projectileMaxLifetime;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public GameObject MuzzleFlashPrefab => _muzzleFlashPrefab;
        public GameObject ImpactEffectPrefab => _impactEffectPrefab;
        public float KickBackDistance => _kickBackDistance;
        public float KickUpAngle => _kickUpAngle;
        public float KickDuration => _kickDuration;
        public int KickVibrato => _kickVibrato;
        public Sprite Icon => _icon;
        public AudioClip FireSound => _fireSound;
    }
}
