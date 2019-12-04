using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface ISubscriber
    {
        void Subscribe<TEvent, TEventHandler>() where TEventHandler : IEventHandler<TEvent> where TEvent : IEventBase;

        void SubscribeDynamic<TEventHandler>() where TEventHandler : IDynamicEventHandler;

        bool Unsubscribe<TEvent, TEventHandler>() where TEventHandler : IEventHandler<TEvent> where TEvent : IEventBase;

        bool UnsubscribeDynamic<TEventHandler>() where TEventHandler : IDynamicEventHandler;
    }
}
