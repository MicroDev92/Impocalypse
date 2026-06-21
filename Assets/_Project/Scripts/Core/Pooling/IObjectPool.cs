using UnityEngine;

namespace Core.Pooling
{
    /// <summary>
    /// Generic object pool interface for Unity components.
    /// </summary>
    /// <typeparam name="T">Component type to pool</typeparam>
    public interface IObjectPool<T> where T : Component
    {
        /// <summary>
        /// Get an object from the pool. Creates new if pool is empty.
        /// </summary>
        /// <returns>Pooled object, activated and ready to use</returns>
        T Get();

        /// <summary>
        /// Get an object from the pool and set its parent.
        /// </summary>
        /// <param name="parent">Parent transform for the object</param>
        /// <returns>Pooled object, activated and ready to use</returns>
        T Get(Transform parent);

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="item">Object to return</param>
        void Return(T item);

        /// <summary>
        /// Pre-warm the pool with a number of instances.
        /// </summary>
        /// <param name="count">Number of instances to create</param>
        void Prewarm(int count);

        /// <summary>
        /// Clear all objects in the pool.
        /// </summary>
        /// <param name="destroyObjects">If true, destroys the GameObjects</param>
        void Clear(bool destroyObjects = true);

        /// <summary>
        /// Current number of available objects in pool.
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// Total number of objects created by this pool.
        /// </summary>
        int TotalCreated { get; }
    }
}
