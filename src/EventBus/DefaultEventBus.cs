using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Abstractions;
using EventBus.Events;

namespace EventBus
{
    public class DefaultEventBus : IEventBus
    {
        private readonly IEventStore _eventStore;



        public DefaultEventBus()
        {

        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEventBase
        {
            throw new NotImplementedException();
        }

        public void Subscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            throw new NotImplementedException();
        }

        public void SubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        void ISubscriber.Unsubscribe<TEvent, TEventHandler>()
        {
            throw new NotImplementedException();
        }

        public void UnsubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }
    }
}
