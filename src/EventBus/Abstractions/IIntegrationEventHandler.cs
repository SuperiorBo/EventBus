using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface IIntegrationEventHandler<in TIEvent> : IEventHandler<TIEvent> where TIEvent : IEventBase
    {
    }
}
