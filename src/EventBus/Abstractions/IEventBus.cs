using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface IEventBus : IPublisher, ISubscriber
    {
    }
}
