using MessageBusCore.Events;
using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IGcpPubSub
    {
        #region GCP
        Task PublishGCP(IntegrationEvent @event, string topicId);

        void SubscribeGCP<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        Task SubscriberCreateGCP<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        #endregion
    }
}
