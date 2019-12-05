using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventBus.Abstractions;
using EventBus.Events;

namespace EventBus
{
    public partial class EventStoreInMemory : IEventStore
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public EventStoreInMemory()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }

        public void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEventBase
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = GetEventKey<TEvent>();

            DoAddSubscription(eventName, typeof(TEventHandler), false);

            if (!_eventTypes.Contains(typeof(TEvent)))
                _eventTypes.Add(typeof(TEvent));
        }

        public void AddDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler
        {
            DoAddSubscription(eventName, typeof(TEventHandler), true);
        }


        public void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEventBase
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = GetEventKey<TEvent>();

            var handlerToRemove = FindSubscriptionToRemove(eventName, typeof(TEventHandler));
            RemoveHandler(eventName,handlerToRemove);
        }

        public void RemoveDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler
        {
            var handlerToRemove = FindSubscriptionToRemove(eventName, typeof(TEventHandler));
            RemoveHandler(eventName, handlerToRemove);
        }

        public bool HasSubscriptionsForEvent<TEvent>() where TEvent : IEventBase
        {
            var key = GetEventKey<TEvent>();
            return HasSubscriptionsForEvent(key);
        }

        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(x => x.Name == eventName);


        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<TEvent>() where TEvent : IEventBase
        {
            var eventName = GetEventKey<TEvent>();
            return GetHandlersForEvent(eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];


        public void Clear()
        {
            _eventTypes.Clear();
            _handlers.Clear();
        }

        public string GetEventKey<T>() => typeof(T).Name;

        public bool IsEmpty() => !_handlers.Keys.Any();

        #region Private Method


        private void DoAddSubscription(string eventName, Type handlerType, bool isDynamic)
        {
            if (!HasSubscriptionsForEvent(eventName))
                _handlers.Add(eventName, new List<SubscriptionInfo>());

            if (_handlers[eventName].Any(x => x.IsDynamic == false && x.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }

            if (isDynamic)
                _handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
            else
                _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName))
                return null;

            return _handlers[eventName].SingleOrDefault(x => x.HandlerType == handlerType);
        }

        private void RemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            _handlers[eventName].Remove(subsToRemove);
            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(x => x.Name == eventName);
                if (eventType != null)
                    _eventTypes.Remove(eventType);
                RaiseOnEventRemoved(eventName);
            }


        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        #endregion
    }
}
