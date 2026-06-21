using System;

namespace Core.Events
{
    /// <summary>
    /// Marker interface for all game events.
    /// </summary>
    public interface IGameEvent { }

    /// <summary>
    /// Event bus interface for publish/subscribe pattern.
    /// Provides decoupled communication between game systems.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Subscribe to events of type T.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to</typeparam>
        /// <param name="handler">Handler to call when event is published</param>
        void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent;

        /// <summary>
        /// Unsubscribe from events of type T.
        /// </summary>
        /// <typeparam name="T">Event type to unsubscribe from</typeparam>
        /// <param name="handler">Handler to remove</param>
        void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent;

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <typeparam name="T">Event type to publish</typeparam>
        /// <param name="gameEvent">Event data to publish</param>
        void Publish<T>(T gameEvent) where T : struct, IGameEvent;

        /// <summary>
        /// Check if there are any subscribers for event type T.
        /// </summary>
        /// <typeparam name="T">Event type to check</typeparam>
        /// <returns>True if there are subscribers</returns>
        bool HasSubscribers<T>() where T : struct, IGameEvent;

        /// <summary>
        /// Clear all subscriptions. Use with caution.
        /// </summary>
        void ClearAllSubscriptions();
    }
}
