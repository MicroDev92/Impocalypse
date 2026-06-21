using UnityEngine;

namespace _Project.Scripts.Utility
{
    /// <summary>
    /// Helper class for initializing and managing services in Unity scenes.
    /// Provides debugging information and ensures proper service setup.
    /// </summary>
    public class ServiceInitializer : MonoBehaviour
    {
        [Header("Debug Options")]
        public bool logServiceRegistration = true;
        public bool validateServicesOnStart = true;
    
        [Header("Required Services")]
        public GameObject[] servicePrefabs;
    
        private void Start()
        {
            if (logServiceRegistration)
            {
                Debug.Log("=== Service Initializer Starting ===");
            }
        
            if (validateServicesOnStart)
            {
                ValidateRequiredServices();
            }
        }
    
        /// <summary>
        /// Validates that all required services are available
        /// </summary>
        private void ValidateRequiredServices()
        {
            var requiredServices = new[]
            {
                typeof(IGameManager),
                typeof(INetworkService),
                typeof(IGridService)
            };
        
            foreach (var serviceType in requiredServices)
            {
                if (!ServiceLocator.HasService(serviceType))
                {
                    Debug.LogWarning($"Required service not found: {serviceType.Name}");
                }
                else if (logServiceRegistration)
                {
                    Debug.Log($"✓ Service available: {serviceType.Name}");
                }
            }
        
            if (logServiceRegistration)
            {
                var registeredServices = ServiceLocator.GetRegisteredServiceTypes();
                Debug.Log($"Total registered services: {registeredServices.Length}");
                foreach (var service in registeredServices)
                {
                    Debug.Log($"  - {service.Name}");
                }
            }
        }
    
        /// <summary>
        /// Manually register a service (useful for testing)
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="service">Service instance</param>
        public void RegisterService<T>(T service) where T : class
        {
            ServiceLocator.RegisterService(service);
            if (logServiceRegistration)
            {
                Debug.Log($"Manually registered service: {typeof(T).Name}");
            }
        }
    
        /// <summary>
        /// Get a service with error handling
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance or null</returns>
        public T GetService<T>() where T : class
        {
            var service = ServiceLocator.GetService<T>();
            if (service == null)
            {
                Debug.LogWarning($"Service not found: {typeof(T).Name}");
            }
            return service;
        }
    
        /// <summary>
        /// Check if a service is available
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if service is available</returns>
        public bool HasService<T>() where T : class
        {
            return ServiceLocator.HasService<T>();
        }
    
        /// <summary>
        /// Clear all services (useful for scene transitions)
        /// </summary>
        public void ClearAllServices()
        {
            ServiceLocator.ClearAllServices();
            if (logServiceRegistration)
            {
                Debug.Log("All services cleared");
            }
        }
    
        /// <summary>
        /// Log current service status for debugging
        /// </summary>
        [ContextMenu("Log Service Status")]
        public void LogServiceStatus()
        {
            Debug.Log("=== Service Status ===");
            var services = ServiceLocator.GetRegisteredServiceTypes();
        
            if (services.Length == 0)
            {
                Debug.Log("No services registered");
                return;
            }
        
            foreach (var serviceType in services)
            {
                var service = ServiceLocator.GetService(serviceType);
                string status = service != null ? "✓ Available" : "✗ Not instantiated";
                Debug.Log($"{serviceType.Name}: {status}");
            }
        }
    
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && logServiceRegistration)
            {
                Debug.Log("Application paused - services remain registered");
            }
        }
    
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && logServiceRegistration)
            {
                Debug.Log("Application focused - validating services");
                ValidateRequiredServices();
            }
        }
    }
}
