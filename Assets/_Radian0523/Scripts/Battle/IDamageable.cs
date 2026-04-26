using UnityEngine;

namespace Velora.Battle
{
    /// <summary>
    /// ダメージ受付インターフェース。
    /// 武器→敵、敵→プレイヤーの両方向で共通のダメージ適用契約を定義する。
    /// MonoBehaviour に実装することで、Collider 経由の TryGetComponent で取得可能。
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector3 hitPoint, bool isHeadshot);
    }
}
