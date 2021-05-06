using MessageBusCore.Events;
using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IEventBus
    {
        #region Azure

        Task PublishAzure(IntegrationEvent @event);

        Task SubscribeAzure<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        Task SubscriberCreateAzure<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void SubscribeDynamicAzure<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        void UnsubscribeDynamicAzure<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        void UnsubscribeAzure<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;

        #endregion
    }
}