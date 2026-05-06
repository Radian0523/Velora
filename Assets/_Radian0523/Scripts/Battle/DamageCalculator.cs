using UnityEngine;
using Velora.Data;

namespace Velora.Battle
{
    /// <summary>
    /// ヒット情報。射撃ストラテジーから受け取り、ダメージ計算に渡す。
    /// readonly struct で GC Alloc を回避。
    /// </summary>
    public readonly struct HitInfo
    {
        public Vector3 HitPoint { get; }
        public Vector3 HitNormal { get; }
        public bool IsHeadshot { get; }
        public Collider HitCollider { get; }

        public HitInfo(Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot, Collider hitCollider)
        {
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            IsHeadshot = isHeadshot;
            HitCollider = hitCollider;
        }
    }

    /// <summary>
    /// ダメージ計算結果。UI 表示やエフェクト分岐に使用する。
    /// </summary>
    public readonly struct DamageResult
    {
        public float FinalDamage { get; }
        public bool IsHeadshot { get; }

        public DamageResult(float finalDamage, bool isHeadshot)
        {
            FinalDamage = finalDamage;
            IsHeadshot = isHeadshot;
        }
    }

    /// <summary>
    /// ダメージ計算を Battle 層に集約する static クラス。
    /// 武器の基礎ダメージ × ヘッドショット倍率 × バフ倍率で最終ダメージを算出。
    /// 計算ロジックを一箇所に集めることで、バランス調整時の変更箇所を限定する。
    /// HitscanStrategy・Projectile の両方がこのクラスを経由してダメージを計算する。
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 直撃ダメージを算出する。ヘッドショット時は追加倍率を適用。
        /// </summary>
        public static DamageResult Calculate(WeaponData weaponData, float damageMultiplier, bool isHeadshot)
        {
            float baseDamage = weaponData.Damage * damageMultiplier;

            float finalDamage = isHeadshot
                ? baseDamage * weaponData.HeadshotMultiplier
                : baseDamage;

            return new DamageResult(finalDamage, isHeadshot);
        }

        /// <summary>
        /// スプラッシュダメージを算出する。距離減衰(falloff)を適用。
        /// </summary>
        public static float CalculateSplash(WeaponData weaponData, float damageMultiplier, float falloff)
        {
            return weaponData.Damage * damageMultiplier * weaponData.SplashDamageMultiplier * falloff;
        }
    }
}
