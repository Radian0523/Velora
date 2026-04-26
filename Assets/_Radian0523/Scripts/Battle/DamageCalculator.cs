using UnityEngine;
using Velora.Data;
using Velora.Player;

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
    /// 武器の基礎ダメージ × ヘッドショット倍率 × プレイヤーのバフ倍率で最終ダメージを算出。
    /// 計算ロジックを一箇所に集めることで、バランス調整時の変更箇所を限定する。
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Calculate(WeaponData weaponData, HitInfo hitInfo, PlayerModel playerModel)
        {
            float baseDamage = weaponData.Damage * playerModel.DamageMultiplier;

            float finalDamage = hitInfo.IsHeadshot
                ? baseDamage * weaponData.HeadshotMultiplier
                : baseDamage;

            return new DamageResult(finalDamage, hitInfo.IsHeadshot);
        }
    }
}
