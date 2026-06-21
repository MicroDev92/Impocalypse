using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Utility
{
    /// <summary>
    /// Service Locator pattern implementation for dependency management.
    /// Provides a centralized registry for game services, reducing tight coupling.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();
        private static readonly Dictionary<Type, Func<object>> ServiceFactories = new();

        private static bool _loggingEnabled = true;

        /// <summary>
        /// Enable or disable service registration logging.
        /// </summary>
        public static bool LoggingEnabled
        {
            get => _loggingEnabled;
            set => _loggingEnabled = value;
        }

        /// <summary>
        /// Registers a service instance with the service locator.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <param name="service">The service instance to register</param>
        public static void RegisterService<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Cannot register null service of type {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            bool isReplacement = Services.ContainsKey(type);

            Services[type] = service;

            if (_loggingEnabled)
            {
                var action = isReplacement ? "replaced" : "registered";
                Debug.Log($"[ServiceLocator] Service {action}: {type.Name}");
            }
        }

        /// <summary>
        /// Registers a factory method for creating service instances on demand.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <param name="factory">Factory method to create the service</param>
        public static void RegisterServiceFactory<T>(Func<T> factory) where T : class
        {
            if (factory == null)
            {
                Debug.LogError($"Cannot register null factory for type {typeof(T).Name}");
                return;
            }

            ServiceFactories[typeof(T)] = () => factory();
            Debug.Log($"Service factory registered: {typeof(T).Name}");
        }

        /// <summary>
        /// Gets a service instance. Creates it using factory if not already instantiated.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <returns>The service instance, or null if not found</returns>
        public static T GetService<T>() where T : class
        {
            Type serviceType = typeof(T);

            // Check if service is already instantiated
            if (Services.TryGetValue(serviceType, out var existingService))
            {
                return (T)existingService;
            }

            // Try to create service using factory
            if (ServiceFactories.TryGetValue(serviceType, out var factory))
            {
                var newService = factory();
                if (newService != null)
                {
                    Services[serviceType] = newService;
                    if (_loggingEnabled)
                    {
                        Debug.Log($"[ServiceLocator] Service created via factory: {serviceType.Name}");
                    }
                    return (T)newService;
                }
            }

            Debug.LogWarning($"[ServiceLocator] Service not found: {serviceType.Name}");
            return null;
        }

        /// <summary>
        /// Tries to get a service instance without logging warnings if not found.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <param name="service">The service instance if found</param>
        /// <returns>True if service was found, false otherwise</returns>
        public static bool TryGetService<T>(out T service) where T : class
        {
            Type serviceType = typeof(T);

            // Check if service is already instantiated
            if (Services.TryGetValue(serviceType, out var existingService))
            {
                service = (T)existingService;
                return true;
            }

            // Try to create service using factory
            if (ServiceFactories.TryGetValue(serviceType, out var factory))
            {
                var newService = factory();
                if (newService != null)
                {
                    Services[serviceType] = newService;
                    if (_loggingEnabled)
                    {
                        Debug.Log($"[ServiceLocator] Service created via factory: {serviceType.Name}");
                    }
                    service = (T)newService;
                    return true;
                }
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Gets a service or throws an exception if not found.
        /// Use when a service is required and missing indicates a bug.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown if service is not registered</exception>
        public static T GetRequiredService<T>() where T : class
        {
            if (TryGetService<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] Required service not found: {typeof(T).Name}. " +
                "Ensure the service is registered before accessing it.");
        }

        /// <summary>
        /// Checks if a service is registered or can be created.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        /// <returns>True if service is available</returns>
        public static bool HasService<T>() where T : class
        {
            Type serviceType = typeof(T);
            return Services.ContainsKey(serviceType) || ServiceFactories.ContainsKey(serviceType);
        }

        /// <summary>
        /// Checks if a service is registered or can be created (non-generic version).
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <returns>True if service is available</returns>
        public static bool HasService(Type serviceType)
        {
            if (serviceType == null) return false;
            return Services.ContainsKey(serviceType) || ServiceFactories.ContainsKey(serviceType);
        }

        /// <summary>
        /// Gets a service instance by type (non-generic version).
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <returns>The service instance, or null if not found</returns>
        public static object GetService(Type serviceType)
        {
            if (serviceType == null) return null;

            if (Services.TryGetValue(serviceType, out var existingService))
            {
                return existingService;
            }

            if (ServiceFactories.TryGetValue(serviceType, out var factory))
            {
                var newService = factory();
                if (newService != null)
                {
                    Services[serviceType] = newService;
                    if (_loggingEnabled)
                    {
                        Debug.Log($"[ServiceLocator] Service created via factory: {serviceType.Name}");
                    }
                    return newService;
                }
            }

            return null;
        }

        /// <summary>
        /// Unregisters a service and its factory if present.
        /// </summary>
        /// <typeparam name="T">The service interface or type</typeparam>
        public static void UnregisterService<T>() where T : class
        {
            Type serviceType = typeof(T);
            bool removed = Services.Remove(serviceType);
            bool factoryRemoved = ServiceFactories.Remove(serviceType);

            if (removed || factoryRemoved)
            {
                Debug.Log($"Service unregistered: {serviceType.Name}");
            }
        }

        /// <summary>
        /// Clears all registered services and factories.
        /// Useful for cleanup between scenes or game sessions.
        /// </summary>
        public static void ClearAllServices()
        {
            Services.Clear();
            ServiceFactories.Clear();
            Debug.Log("All services cleared from ServiceLocator");
        }

        /// <summary>
        /// Gets all registered service types for debugging purposes.
        /// </summary>
        /// <returns>Array of registered service types</returns>
        public static Type[] GetRegisteredServiceTypes()
        {
            var types = new List<Type>();
            types.AddRange(Services.Keys);
            types.AddRange(ServiceFactories.Keys);
            return types.ToArray();
        }
    }
}
