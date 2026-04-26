using UnityEngine;
using Velora.Data;
using Velora.Enemy;
using Velora.Player;

namespace Velora.Core
{
    /// <summary>
    /// テスト用のシーン初期化スクリプト。
    /// PlayerModel の生成と各コンポーネントの Initialize 呼び出しを行う。
    /// WaveDirector / LifetimeScope 実装後に置き換える。
    /// </summary>
    public class BattleTestInitializer : MonoBehaviour
    {
        [Header("プレイヤー")]
        [SerializeField] private PlayerDamageReceiver _playerDamageReceiver;
        [SerializeField] private float _playerMaxHealth = 100f;

        [Header("敵")]
        [SerializeField] private EnemyController[] _enemies;
        [SerializeField] private EnemyData _enemyData;

        private void Start()
        {
            var playerModel = new PlayerModel(_playerMaxHealth);
            _playerDamageReceiver.Initialize(playerModel);

            var playerTransform = _playerDamageReceiver.transform;

            foreach (var enemy in _enemies)
            {
                if (enemy == null) continue;
                enemy.Initialize(_enemyData, playerTransform, _playerDamageReceiver);
            }
        }
    }
}
