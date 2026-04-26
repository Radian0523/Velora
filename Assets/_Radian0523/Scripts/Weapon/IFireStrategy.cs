using Cysharp.Threading.Tasks;
using UnityEngine;
using Velora.Data;

namespace Velora.Weapon
{
    /// <summary>
    /// 射撃方式のストラテジーインターフェース。
    /// Hitscan（レイキャスト即着弾）と Projectile（弾丸飛行）を同一の呼び出し口で扱う。
    /// 新しい射撃方式を追加する場合はこのインターフェースを実装するだけでよい。
    /// </summary>
    public interface IFireStrategy
    {
        /// <param name="spreadAngle">適用する拡散角。ADS 状態に応じて呼び出し側が決定する。</param>
        /// <returns>ヒット情報。エフェクト生成は呼び出し側が FireResult を基に行う。</returns>
        UniTask<FireResult> Fire(WeaponData data, Transform muzzle, LayerMask hitMask, float spreadAngle);
    }
}
