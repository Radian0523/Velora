using UnityEngine;

namespace Velora.Data
{
    /// <summary>
    /// プレイヤーの初期設定値を管理する ScriptableObject。
    /// LifetimeScope から設定値を分離し、設定の一元管理を実現する。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Velora/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("体力")]
        [SerializeField] private float _initialMaxHealth = 100f;

        public float InitialMaxHealth => _initialMaxHealth;
    }
}
