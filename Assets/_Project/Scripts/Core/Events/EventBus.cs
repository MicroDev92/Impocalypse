using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Utility;

namespace Core.Events
{
    /// <summary>
    /// Event bus implementation using publish/subscribe pattern.
    /// Provides decoupled communication between game systems.
    /// Thread-safe for Unity's main thread usage.
    /// </summary>
    public class EventBus : MonoBehaviour, IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private readonly Queue<Action> _pendingActions = new();
        private bool _isPublishing;

        [SerializeField] private bool logEvents = false;

        private void Awake()
        {
            ServiceLocator.RegisterService<IEventBus>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService<IEventBus>();
            ClearAllSubscriptions();
        }

        public void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null)
            {
                Debug.LogWarning($"[EventBus] Attempted to subscribe null handler for {typeof(T).Name}");
                return;
            }

            var eventType = typeof(T);

            // If we're currently publishing, queue the subscription
            if (_isPublishing)
            {
                _pendingActions.Enqueue(() => SubscribeInternal<T>(handler));
                return;
            }

            SubscribeInternal<T>(handler);
        }

        private void SubscribeInternal<T>(Action<T> handler) where T : struct, IGameEvent
        {
            var eventType = typeof(T);

            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[eventType] = handlers;
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);

                if (logEvents)
                {
                    Debug.Log($"[EventBus] Subscribed to {eventType.Name}. Total subscribers: {handlers.Count}");
                }
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;

            // If we're currently publishing, queue the unsubscription
            if (_isPublishing)
            {
                _pendingActions.Enqueue(() => UnsubscribeInternal<T>(handler));
                return;
            }

            UnsubscribeInternal<T>(handler);
        }

        private void UnsubscribeInternal<T>(Action<T> handler) where T : struct, IGameEvent
        {
            var eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);

                if (logEvents)
                {
                    Debug.Log($"[EventBus] Unsubscribed from {eventType.Name}. Remaining subscribers: {handlers.Count}");
                }

                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }

        public void Publish<T>(T gameEvent) where T : struct, IGameEvent
        {
            var eventType = typeof(T);

            if (logEvents)
            {
                Debug.Log($"[EventBus] Publishing {eventType.Name}");
            }

            if (!_subscribers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                if (logEvents)
                {
                    Debug.Log($"[EventBus] No subscribers for {eventType.Name}");
                }
                return;
            }

            _isPublishing = true;

            try
            {
                // Create a copy to avoid modification during iteration
                var handlersCopy = new List<Delegate>(handlers);

                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        ((Action<T>)handler)?.Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventBus] Error in handler for {eventType.Name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            finally
            {
                _isPublishing = false;

                // Process any pending subscribe/unsubscribe actions
                while (_pendingActions.Count > 0)
                {
                    _pendingActions.Dequeue()?.Invoke();
                }
            }
        }

        public bool HasSubscribers<T>() where T : struct, IGameEvent
        {
            var eventType = typeof(T);
            return _subscribers.TryGetValue(eventType, out var handlers) && handlers.Count > 0;
        }

        public void ClearAllSubscriptions()
        {
            _subscribers.Clear();
            _pendingActions.Clear();

            if (logEvents)
            {
                Debug.Log("[EventBus] All subscriptions cleared");
            }
        }

        /// <summary>
        /// Gets the number of subscribers for a specific event type.
        /// Useful for debugging.
        /// </summary>
        public int GetSubscriberCount<T>() where T : struct, IGameEvent
        {
            var eventType = typeof(T);
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// Gets all subscribed event types.
        /// Useful for debugging.
        /// </summary>
        public IEnumerable<Type> GetSubscribedEventTypes()
        {
            return _subscribers.Keys;
        }
    }
}
