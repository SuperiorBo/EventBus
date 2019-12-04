using System;
using EventBus.Abstractions;
using EventBus.Events;

namespace EventBusRabbitMQ
{
    public class EventBusRabbitMQ:IEventBus,IDisposable
    {
        public EventBusRabbitMQ()
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

        public void SubscribeDynamic<TEventHandler>() where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        public bool Unsubscribe<TEvent, TEventHandler>() where TEvent : IEventBase where TEventHandler : IEventHandler<TEvent>
        {
            throw new NotImplementedException();
        }

        public bool UnsubscribeDynamic<TEventHandler>() where TEventHandler : IDynamicEventHandler
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
