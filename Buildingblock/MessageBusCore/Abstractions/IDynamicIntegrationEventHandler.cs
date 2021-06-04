using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}