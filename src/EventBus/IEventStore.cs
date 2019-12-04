using System;
using System.Collections.Generic;
using EventBus.Abstractions;
using EventBus.Events;
using static EventBus.EventStoreInMemory;

namespace EventBus
{
    public interface IEventStore
    {

        event EventHandler<string> OnEventRemoved;

        bool IsEmpty();

        void AddSubscription<TEvent, TEventHandler>() 
            where TEventHandler : IEventHandler<TEvent>
            where TEvent : IEventBase;

        void AddDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler;

        void RemoveSubscription<TEvent, TEventHandler>()
            where TEventHandler : IEventHandler<TEvent>
            where TEvent : IEventBase;

        void RemoveDynamicSubscription<TEventHandler>(string eventName)
            where TEventHandler : IDynamicEventHandler;


        bool HasSubscriptionsForEvent<TEvent>()
            where TEvent : IEventBase;

        bool HasSubscriptionsForEvent(string eventName);

        Type GetEventTypeByName(string eventName);

        IEnumerable<SubscriptionInfo> GetHandlersForEvent<TEvent>()
            where TEvent : IEventBase;

        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

        void Clear();

        string GetEventKey<T>();
    }
}
