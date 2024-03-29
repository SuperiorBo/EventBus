﻿using System;
using System.Collections.Generic;
using System.Text;
using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface ISubscriber
    {
        void Subscribe<TEvent, TEventHandler>() where TEventHandler : IEventHandler<TEvent> where TEvent : IEventBase;

        void SubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler;

        void Unsubscribe<TEvent, TEventHandler>() where TEventHandler : IEventHandler<TEvent> where TEvent : IEventBase;

        void UnsubscribeDynamic<TEventHandler>(string eventName) where TEventHandler : IDynamicEventHandler;
    }
}
