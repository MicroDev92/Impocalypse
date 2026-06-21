using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Pooling
{
    /// <summary>
    /// Generic object pool for Unity components.
    /// Reduces garbage collection by reusing objects instead of creating/destroying.
    /// </summary>
    /// <typeparam name="T">Component type to pool</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new();
        private readonly Func<T> _factory;
        private readonly Transform _poolParent;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;
        private readonly int _maxSize;

        public int AvailableCount => _pool.Count;
        public int TotalCreated { get; private set; }

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        /// <param name="factory">Factory function to create new instances</param>
        /// <param name="poolParent">Parent transform for pooled objects (optional)</param>
        /// <param name="onGet">Action to perform when getting object from pool (optional)</param>
        /// <param name="onReturn">Action to perform when returning object to pool (optional)</param>
        /// <param name="initialSize">Initial pool size (optional)</param>
        /// <param name="maxSize">Maximum pool size, 0 = unlimited (optional)</param>
        public ObjectPool(
            Func<T> factory,
            Transform poolParent = null,
            Action<T> onGet = null,
            Action<T> onReturn = null,
            int initialSize = 0,
            int maxSize = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _poolParent = poolParent;
            _onGet = onGet;
            _onReturn = onReturn;
            _maxSize = maxSize;

            if (initialSize > 0)
            {
                Prewarm(initialSize);
            }
        }

        /// <summary>
        /// Create a pool from a prefab.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="poolParent">Parent for pooled objects</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size</param>
        /// <returns>New object pool</returns>
        public static ObjectPool<T> FromPrefab(
            T prefab,
            Transform poolParent = null,
            int initialSize = 0,
            int maxSize = 0)
        {
            return new ObjectPool<T>(
                factory: () =>
                {
                    var instance = UnityEngine.Object.Instantiate(prefab);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                poolParent: poolParent,
                onGet: item =>
                {
                    item.gameObject.SetActive(true);
                },
                onReturn: item =>
                {
                    item.gameObject.SetActive(false);
                    if (poolParent != null)
                    {
                        item.transform.SetParent(poolParent);
                    }
                },
                initialSize: initialSize,
                maxSize: maxSize
            );
        }

        public T Get()
        {
            T item;

            if (_pool.Count > 0)
            {
                item = _pool.Dequeue();
            }
            else
            {
                item = _factory();
                TotalCreated++;
            }

            _onGet?.Invoke(item);
            return item;
        }

        public T Get(Transform parent)
        {
            var item = Get();
            item.transform.SetParent(parent);
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                Debug.LogWarning("[ObjectPool] Attempted to return null item");
                return;
            }

            // Check max size
            if (_maxSize > 0 && _pool.Count >= _maxSize)
            {
                // Pool is full, destroy the object
                UnityEngine.Object.Destroy(item.gameObject);
                return;
            }

            _onReturn?.Invoke(item);

            if (_poolParent != null)
            {
                item.transform.SetParent(_poolParent);
            }

            _pool.Enqueue(item);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = _factory();
                TotalCreated++;

                item.gameObject.SetActive(false);

                if (_poolParent != null)
                {
                    item.transform.SetParent(_poolParent);
                }

                _pool.Enqueue(item);
            }
        }

        public void Clear(bool destroyObjects = true)
        {
            if (destroyObjects)
            {
                while (_pool.Count > 0)
                {
                    var item = _pool.Dequeue();
                    if (item != null)
                    {
                        UnityEngine.Object.Destroy(item.gameObject);
                    }
                }
            }
            else
            {
                _pool.Clear();
            }
        }
    }
}
