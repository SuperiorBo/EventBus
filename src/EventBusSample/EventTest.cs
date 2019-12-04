using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventBusSample
{
    //internal class EventTest
    //{
    //    public static void MainTest()
    //    {
    //        var eventBus = DependencyResolver.Current.ResolveService<IEventBus>();
    //        eventBus.Subscribe<CounterEvent, CounterEventHandler1>();
    //        eventBus.Subscribe<CounterEvent, CounterEventHandler2>();
    //        eventBus.Subscribe<CounterEvent, DelegateEventHandler<CounterEvent>>();
    //        eventBus.Publish(new CounterEvent { Counter = 1 });

    //        eventBus.Unsubscribe<CounterEvent, CounterEventHandler1>();
    //        eventBus.Unsubscribe<CounterEvent, DelegateEventHandler<CounterEvent>>();
    //        eventBus.Publish(new CounterEvent { Counter = 2 });
    //    }
    //}

    internal class CounterEvent : EventBase
    {
        public int Counter { get; set; }
        public DateTimeOffset EventAt { get; }
        public string EventId { get; }
    }

    internal class CounterEventHandler1 : IEventHandler<CounterEvent>
    {
        private readonly ILogger<CounterEventHandler1> _logger;
        public Task Handle(CounterEvent @event)
        {
            _logger.LogInformation($"Event Info: {JsonConvert.SerializeObject(@event)}, Handler Type:{GetType().FullName}");
            return Task.CompletedTask;
        }
    }

    internal class CounterEventHandler2 : IEventHandler<CounterEvent>
    {
        private readonly ILogger<CounterEventHandler2> _logger;

        public CounterEventHandler2(
            ILogger<CounterEventHandler2> logger
        )
        {
            _logger = logger;
        }

        public Task Handle(CounterEvent @event)
        {
            _logger.LogInformation($"Event Info: {JsonConvert.SerializeObject(@event)}, Handler Type:{GetType().FullName}");
            return Task.CompletedTask;
        }
    }
}
