using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface IPublisher
    {
        void Publish<TEvent>(TEvent @event) where TEvent : IEventBase;
    }
}
