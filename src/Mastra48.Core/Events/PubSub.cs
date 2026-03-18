using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mastra48.Events
{
    /// <summary>
    /// Represents a published event.
    /// Mirrors Event from packages/core/src/events/types.ts
    /// </summary>
    public class MastraEvent
    {
        public string Topic { get; set; }
        public object Payload { get; set; }
        public DateTime Timestamp { get; set; }
        public string Id { get; set; }
    }

    /// <summary>
    /// Event handler delegate.
    /// </summary>
    public delegate Task EventHandler(MastraEvent evt, Func<Task> callback);

    /// <summary>
    /// Pub/sub interface for event-driven communication.
    /// Mirrors PubSub from packages/core/src/events/pubsub.ts
    /// </summary>
    public interface IPubSub
    {
        void Subscribe(string topic, EventHandler handler);
        void Unsubscribe(string topic, EventHandler handler);
        Task PublishAsync(string topic, MastraEvent evt);
    }

    /// <summary>
    /// In-process event emitter based pub/sub implementation.
    /// Mirrors EventEmitterPubSub from packages/core/src/events/event-emitter.ts
    /// </summary>
    public class EventEmitterPubSub : IPubSub
    {
        private readonly Dictionary<string, List<EventHandler>> _handlers
            = new Dictionary<string, List<EventHandler>>();

        public void Subscribe(string topic, EventHandler handler)
        {
            if (!_handlers.ContainsKey(topic))
                _handlers[topic] = new List<EventHandler>();

            _handlers[topic].Add(handler);
        }

        public void Unsubscribe(string topic, EventHandler handler)
        {
            if (_handlers.ContainsKey(topic))
                _handlers[topic].Remove(handler);
        }

        public async Task PublishAsync(string topic, MastraEvent evt)
        {
            if (!_handlers.ContainsKey(topic)) return;

            foreach (var handler in _handlers[topic])
            {
                await handler(evt, () => Task.FromResult(0));
            }
        }
    }
}
