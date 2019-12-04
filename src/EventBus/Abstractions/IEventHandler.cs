using System.Threading.Tasks;
using EventBus.Events;

namespace EventBus.Abstractions
{
    public interface IEventHandler<in TIEvent> : IEventHandler
        where TIEvent : IEventBase
    {
        Task Handle(TIEvent @event);
    }

    public interface IEventHandler
    {
    }
}
