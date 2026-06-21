using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;
using Core.Pooling;

namespace Imps
{
    /// <summary>
    /// Factory for creating and managing imp instances.
    /// Uses object pooling to reduce GC pressure.
    /// </summary>
    public class ImpFactory : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject impPrefab;
        [SerializeField] private Transform impContainer;
        [SerializeField] private int poolInitialSize = 50;

        private ObjectPool<Imp> _impPool;
        private readonly List<Imp> _activeImps = new();

        public IReadOnlyList<Imp> ActiveImps => _activeImps;

        private void Awake()
        {
            ServiceLocator.RegisterService<ImpFactory>(this);
            InitializePool();
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService<ImpFactory>();
        }

        private void InitializePool()
        {
            if (impPrefab == null)
            {
                // Create a default imp prefab
                impPrefab = CreateDefaultImpPrefab();
            }

            var impComponent = impPrefab.GetComponent<Imp>();
            if (impComponent == null)
            {
                impComponent = impPrefab.AddComponent<Imp>();
            }

            _impPool = ObjectPool<Imp>.FromPrefab(
                impComponent,
                impContainer ?? transform,
                poolInitialSize
            );
        }

        private GameObject CreateDefaultImpPrefab()
        {
            var go = new GameObject("ImpPrefab");
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<Imp>();
            go.SetActive(false);
            go.transform.SetParent(transform);
            return go;
        }

        /// <summary>
        /// Creates an imp at the specified position.
        /// </summary>
        /// <param name="coordinate">Grid coordinate</param>
        /// <param name="ownerActorNumber">Owner's actor number</param>
        /// <param name="colorId">Color identifier</param>
        /// <param name="sprite">Sprite to display</param>
        /// <param name="position">World position</param>
        /// <returns>Created imp</returns>
        public Imp CreateImp(string coordinate, int ownerActorNumber, string colorId, Sprite sprite, Vector3 position)
        {
            if (_impPool == null)
            {
                Debug.LogError("[ImpFactory] Pool not initialized!");
                return null;
            }

            var imp = _impPool.Get();
            if (imp == null)
            {
                Debug.LogError("[ImpFactory] Failed to get imp from pool");
                return null;
            }

            imp.Initialize(coordinate, ownerActorNumber, colorId, sprite, position);
            imp.transform.SetParent(impContainer ?? transform, true);
            _activeImps.Add(imp);

            return imp;
        }

        /// <summary>
        /// Returns an imp to the pool.
        /// </summary>
        public void ReturnImp(Imp imp)
        {
            if (imp == null) return;

            _activeImps.Remove(imp);
            imp.Reset();
            _impPool?.Return(imp);
        }

        /// <summary>
        /// Returns all active imps to the pool.
        /// </summary>
        public void ReturnAllImps()
        {
            foreach (var imp in _activeImps.ToArray())
            {
                imp.Reset();
                _impPool?.Return(imp);
            }
            _activeImps.Clear();

            Debug.Log("[ImpFactory] Returned all imps to pool");
        }

        /// <summary>
        /// Gets imps owned by a specific player.
        /// </summary>
        public List<Imp> GetImpsByOwner(int ownerActorNumber)
        {
            var result = new List<Imp>();
            foreach (var imp in _activeImps)
            {
                if (imp.OwnerActorNumber == ownerActorNumber)
                {
                    result.Add(imp);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets an imp at a specific coordinate.
        /// </summary>
        public Imp GetImpAtCoordinate(string coordinate)
        {
            foreach (var imp in _activeImps)
            {
                if (imp.Coordinate == coordinate)
                {
                    return imp;
                }
            }
            return null;
        }
    }
}
