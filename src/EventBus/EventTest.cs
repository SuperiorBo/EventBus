using System.Threading.Tasks;
using EventBus.Abstractions;
using EventBus.Events;

namespace EventBus
{
    internal class EventTest
    {
        public static void MainTest()
        {
            var eventBus = DependencyResolver.Current.ResolveService<IEventBus>();
            eventBus.Subscribe<CounterEvent, CounterEventHandler1>();
            eventBus.Subscribe<CounterEvent, CounterEventHandler2>();
            eventBus.Subscribe<CounterEvent, DelegateEventHandler<CounterEvent>>();
            eventBus.Publish(new CounterEvent { Counter = 1 });

            eventBus.Unsubscribe<CounterEvent, CounterEventHandler1>();
            eventBus.Unsubscribe<CounterEvent, DelegateEventHandler<CounterEvent>>();
            eventBus.Publish(new CounterEvent { Counter = 2 });
        }
    }

    internal class CounterEvent : IEventBase
    {
        public int Counter { get; set; }
    }

    internal class CounterEventHandler1 : IEventHandler<CounterEvent>
    {
        private readonly ILogger<CounterEventHandler1> _logger;
        public Task Handle(CounterEvent @event)
        {
            LogHelper.GetLogger<CounterEventHandler1>().Info($"Event Info: {@event.ToJson()}, Handler Type:{GetType().FullName}");
            return Task.CompletedTask;
        }
    }

    internal class CounterEventHandler2 : IEventHandler<CounterEvent>
    {
        public Task Handle(CounterEvent @event)
        {
            LogHelper.GetLogger<CounterEventHandler2>().Info($"Event Info: {@event.ToJson()}, Handler Type:{GetType().FullName}");
            return Task.CompletedTask;
        }
    }
}
