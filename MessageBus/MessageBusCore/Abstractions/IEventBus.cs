using MessageBusCore.Events;
using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IEventBus
    {

        #region Azure

        void PublishAzure(IntegrationEvent @event, string authHeader = "");

        void SubscribeAzure<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void SubscriberCreateAzure<T, TH>(string subscriber)
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

        #region GCP

        void PublishGCP(IntegrationEvent @event, string authHeader = "");

        void SubscribeGCP<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void SubscriberCreateGCP<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        #endregion

    }
}