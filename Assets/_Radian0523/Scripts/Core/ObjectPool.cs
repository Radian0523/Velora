using System.Collections.Generic;
using UnityEngine;

namespace Velora.Core
{
    /// <summary>
    /// ジェネリックオブジェクトプール。
    /// 弾丸・エフェクトなど頻繁に生成/破棄される Component の再利用で
    /// GC Alloc と Instantiate コストを削減する。
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool;
        private readonly int _maxSize;

        public int CountInactive => _pool.Count;

        public ObjectPool(T prefab, Transform parent, int initialSize, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _pool = new Queue<T>(initialSize);

            for (int i = 0; i < initialSize; i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        /// <summary>
        /// プールから取得する。空の場合は新規生成。
        /// maxSize に達している場合は最も古いオブジェクトを強制回収して再利用する。
        /// </summary>
        public T Get()
        {
            T instance;

            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
            }
            else
            {
                instance = CreateInstance();
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>
        /// プールに返却する。maxSize を超える場合は破棄する。
        /// </summary>
        public void Return(T instance)
        {
            // 使用中に別の親へ移されたオブジェクトをプール階層に戻す
            instance.transform.SetParent(_parent);
            instance.gameObject.SetActive(false);

            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(instance);
            }
            else
            {
                Object.Destroy(instance.gameObject);
            }
        }

        /// <summary>
        /// プール内の全オブジェクトを破棄する。シーン遷移時のクリーンアップに使用。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var instance = _pool.Dequeue();
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            return instance;
        }
    }
}
