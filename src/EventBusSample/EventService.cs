using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventBus.Abstractions;
using EventBus.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventBusSample
{
    internal class EventService:IHostedService
    {
        private readonly ILogger<EventService> _logger;
        private readonly IEventBus _eventBus;
        public EventService(
            IEventBus eventBus,
            ILogger<EventService> logger
            )
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        public void MainTest()
        {
            _eventBus.Subscribe<CounterEvent, CounterEventHandler1>();
            _eventBus.Subscribe<CounterEvent, CounterEventHandler2>();
            _eventBus.Publish(new CounterEvent { Counter = 1 });

            _eventBus.Unsubscribe<CounterEvent, CounterEventHandler1>();
            _eventBus.Publish(new CounterEvent { Counter = 2 });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service {nameof(EventService)} Start");
            MainTest();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

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
