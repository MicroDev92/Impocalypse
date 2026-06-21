using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;

namespace Core.Pooling
{
    /// <summary>
    /// Centralized manager for all object pools.
    /// Provides easy access to pools and handles cleanup.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private Transform poolContainer;

        private readonly Dictionary<Type, object> _pools = new();
        private readonly Dictionary<string, object> _namedPools = new();

        private void Awake()
        {
            // Create container if not assigned
            if (poolContainer == null)
            {
                var containerGO = new GameObject("[Pool Container]");
                containerGO.transform.SetParent(transform);
                poolContainer = containerGO.transform;
            }

            ServiceLocator.RegisterService<PoolManager>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService<PoolManager>();
            ClearAllPools();
        }

        /// <summary>
        /// Creates or gets an existing pool for a component type from a prefab.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <returns>Object pool for the component type</returns>
        public IObjectPool<T> GetOrCreatePool<T>(T prefab, int initialSize = 5, int maxSize = 0) where T : Component
        {
            var type = typeof(T);

            if (_pools.TryGetValue(type, out var existingPool))
            {
                return (IObjectPool<T>)existingPool;
            }

            var poolParent = CreatePoolParent(type.Name);
            var pool = ObjectPool<T>.FromPrefab(prefab, poolParent, initialSize, maxSize);
            _pools[type] = pool;

            Debug.Log($"[PoolManager] Created pool for {type.Name} with initial size {initialSize}");
            return pool;
        }

        /// <summary>
        /// Creates or gets a named pool (for multiple pools of same type).
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="name">Unique name for the pool</param>
        /// <param name="prefab">Prefab to pool</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <returns>Object pool</returns>
        public IObjectPool<T> GetOrCreateNamedPool<T>(string name, T prefab, int initialSize = 5, int maxSize = 0) where T : Component
        {
            if (_namedPools.TryGetValue(name, out var existingPool))
            {
                return (IObjectPool<T>)existingPool;
            }

            var poolParent = CreatePoolParent(name);
            var pool = ObjectPool<T>.FromPrefab(prefab, poolParent, initialSize, maxSize);
            _namedPools[name] = pool;

            Debug.Log($"[PoolManager] Created named pool '{name}' with initial size {initialSize}");
            return pool;
        }

        /// <summary>
        /// Gets an existing pool by type.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Pool if exists, null otherwise</returns>
        public IObjectPool<T> GetPool<T>() where T : Component
        {
            var type = typeof(T);
            return _pools.TryGetValue(type, out var pool) ? (IObjectPool<T>)pool : null;
        }

        /// <summary>
        /// Gets an existing named pool.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="name">Pool name</param>
        /// <returns>Pool if exists, null otherwise</returns>
        public IObjectPool<T> GetNamedPool<T>(string name) where T : Component
        {
            return _namedPools.TryGetValue(name, out var pool) ? (IObjectPool<T>)pool : null;
        }

        /// <summary>
        /// Clears a specific pool.
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <param name="destroyObjects">If true, destroys all pooled objects</param>
        public void ClearPool<T>(bool destroyObjects = true) where T : Component
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var pool))
            {
                ((IObjectPool<T>)pool).Clear(destroyObjects);
                _pools.Remove(type);
            }
        }

        /// <summary>
        /// Clears all pools.
        /// </summary>
        /// <param name="destroyObjects">If true, destroys all pooled objects</param>
        public void ClearAllPools(bool destroyObjects = true)
        {
            foreach (var kvp in _pools)
            {
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, new object[] { destroyObjects });
            }
            _pools.Clear();

            foreach (var kvp in _namedPools)
            {
                var clearMethod = kvp.Value.GetType().GetMethod("Clear");
                clearMethod?.Invoke(kvp.Value, new object[] { destroyObjects });
            }
            _namedPools.Clear();

            Debug.Log("[PoolManager] All pools cleared");
        }

        private Transform CreatePoolParent(string name)
        {
            var parent = new GameObject($"Pool_{name}");
            parent.transform.SetParent(poolContainer);
            return parent.transform;
        }

        /// <summary>
        /// Debug: Get pool statistics.
        /// </summary>
        public void LogPoolStats()
        {
            Debug.Log("[PoolManager] Pool Statistics:");
            foreach (var kvp in _pools)
            {
                var pool = kvp.Value;
                var availableCount = (int)pool.GetType().GetProperty("AvailableCount")?.GetValue(pool);
                var totalCreated = (int)pool.GetType().GetProperty("TotalCreated")?.GetValue(pool);
                Debug.Log($"  {kvp.Key.Name}: {availableCount} available, {totalCreated} total created");
            }

            foreach (var kvp in _namedPools)
            {
                var pool = kvp.Value;
                var availableCount = (int)pool.GetType().GetProperty("AvailableCount")?.GetValue(pool);
                var totalCreated = (int)pool.GetType().GetProperty("TotalCreated")?.GetValue(pool);
                Debug.Log($"  [{kvp.Key}]: {availableCount} available, {totalCreated} total created");
            }
        }
    }
}
