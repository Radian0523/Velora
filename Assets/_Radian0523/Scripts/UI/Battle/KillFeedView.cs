using UnityEngine;
using Velora.Core;

namespace Velora.UI
{
    /// <summary>
    /// キルフィードコンテナ。EnemyDiedEvent を購読し、
    /// ObjectPool で KillFeedEntryView を再利用して表示する。
    /// VerticalLayoutGroup を持つ _container 配下にエントリを配置し、
    /// 新しいエントリが上部に積み重なる形式をとる。
    /// </summary>
    public class KillFeedView : MonoBehaviour
    {
        [SerializeField] private KillFeedEntryView _entryPrefab;
        [SerializeField] private Transform _container;

        private const int PoolInitialSize = 3;
        private const int PoolMaxSize = 10;

        private ObjectPool<KillFeedEntryView> _pool;

        private void Start()
        {
            _pool = new ObjectPool<KillFeedEntryView>(
                _entryPrefab, _container, PoolInitialSize, PoolMaxSize);

            EventBus.Subscribe<EnemyDiedEvent>(HandleEnemyDied);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(HandleEnemyDied);
            _pool?.Clear();
        }

        private void HandleEnemyDied(EnemyDiedEvent e)
        {
            var entry = _pool.Get();
            entry.transform.SetParent(_container);

            // 新しいエントリを先頭に配置し、最新のキルが一番上に表示される
            entry.transform.SetAsFirstSibling();

            entry.SetReturnCallback(ReturnEntry);
            entry.Show(e.EnemyName, e.ScoreValue);
        }

        private void ReturnEntry(KillFeedEntryView entry)
        {
            _pool.Return(entry);
        }
    }
}
