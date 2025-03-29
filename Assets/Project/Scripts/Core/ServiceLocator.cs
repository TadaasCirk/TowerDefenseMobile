using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Service locator implementation for accessing global services/managers without direct singleton references.
    /// Provides a central registry for game systems to register and locate each other.
    /// </summary>
    public static class ServiceLocator
    {
        // Dictionary mapping service types to their instances
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        
        // Debug flag to log service registration and requests
        private static bool enableLogging = false;

        /// <summary>
        /// Register a service with the locator
        /// </summary>
        /// <typeparam name="T">Type of the service interface or class</typeparam>
        /// <param name="service">The service instance</param>
        /// <param name="overwriteExisting">Whether to overwrite if a service of this type already exists</param>
        /// <returns>True if registration was successful</returns>
        public static bool Register<T>(T service, bool overwriteExisting = false) where T : class
        {
            Type type = typeof(T);
            
            // Check if service already exists
            if (services.ContainsKey(type) && !overwriteExisting)
            {
                if (enableLogging)
                {
                    Debug.LogWarning($"ServiceLocator: Service of type {type.Name} is already registered and overwriting is not allowed.");
                }
                return false;
            }
            
            // Register or replace the service
            services[type] = service;
            
            if (enableLogging)
            {
                Debug.Log($"ServiceLocator: {type.Name} has been registered.");
            }
            
            return true;
        }

        /// <summary>
        /// Get a service from the locator
        /// </summary>
        /// <typeparam name="T">Type of the service to get</typeparam>
        /// <param name="suppressWarning">Whether to suppress warning if service is not found</param>
        /// <returns>The service instance, or null if not found</returns>
        public static T Get<T>(bool suppressWarning = false) where T : class
        {
            Type type = typeof(T);
            
            if (services.TryGetValue(type, out object service))
            {
                return (T)service;
            }
            
            if (!suppressWarning && enableLogging)
            {
                Debug.LogWarning($"ServiceLocator: Service of type {type.Name} is not registered.");
            }
            
            return null;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">Type of the service to check</typeparam>
        /// <returns>True if the service is registered</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        /// <typeparam name="T">Type of the service to unregister</typeparam>
        /// <returns>True if the service was unregistered</returns>
        public static bool Unregister<T>() where T : class
        {
            Type type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                services.Remove(type);
                
                if (enableLogging)
                {
                    Debug.Log($"ServiceLocator: {type.Name} has been unregistered.");
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Clear all registered services
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            
            if (enableLogging)
            {
                Debug.Log("ServiceLocator: All services have been cleared.");
            }
        }

        /// <summary>
        /// Enable or disable logging for the service locator
        /// </summary>
        /// <param name="enable">Whether to enable logging</param>
        public static void SetLogging(bool enable)
        {
            enableLogging = enable;
        }
    }
}