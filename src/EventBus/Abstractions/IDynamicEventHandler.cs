using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    public interface IDynamicEventHandler:IEventHandler
    {
        Task Handle(dynamic @event);
    }
}
