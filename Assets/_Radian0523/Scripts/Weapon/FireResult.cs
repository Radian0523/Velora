using UnityEngine;

namespace Velora.Weapon
{
    /// <summary>
    /// IFireStrategy の射撃結果。
    /// Strategy はダメージ判定のみ行い、エフェクト生成は呼び出し側（WeaponController）に委ねる。
    /// これにより射撃ロジックと視覚演出の責務を分離できる。
    /// </summary>
    public readonly struct FireResult
    {
        public readonly bool DidHit;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;

        public FireResult(bool didHit, Vector3 hitPoint, Vector3 hitNormal)
        {
            DidHit = didHit;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
        }

        public static FireResult None => new(false, default, default);
    }
}
