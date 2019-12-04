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

        public void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEventBase
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = GetEventKey<TEvent>();
            if(!_handlers[eventName].Any())
                _handlers[eventName] = new List<SubscriptionInfo>();

            var handlerType = typeof(TEventHandler);

            if (_handlers[eventName].Any(x=>x.HandlerType == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
            }

            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
            
            if (!_eventTypes.Contains(typeof(TEvent)))
                _eventTypes.Add(typeof(TEvent));
        }

        public void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEventBase
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = GetEventKey<TEvent>();
            var handlerType = typeof(TEventHandler);
            var handlerToRemove = FindSubscriptionToRemove(eventName, handlerType);
            RemoveHandler(eventName,handlerToRemove);
        }

        public void AddDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler
        {
            if(!_handlers[eventName].Any())
                _handlers[eventName] = new List<SubscriptionInfo>();

            var handlerType = typeof(TEventHandler);

            if(HasSubscriptionsForEvent<TEventHandler>(eventName))



        }

        public void RemoveDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
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
                if(eventType != null)
                    _eventTypes.Remove(eventType);
                RaiseOnEventRemoved(eventName);
            }
                

        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }

        public bool HasSubscriptionsForEvent<TEvent>() where TEvent : IEventBase
        {
            var key = GetEventKey<TEvent>();
            return HasSubscriptionsForEvent(key);
        }

        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<TEvent>() where TEvent : IEventBase
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _eventTypes.Clear();
            _handlers.Clear();
        }

        public string GetEventKey<T>() => typeof(T).Name;

        public bool IsEmpty() => !_handlers.Keys.Any();
    }
}
