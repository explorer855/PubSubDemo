using MessageBusCore.Events;
using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IGcpPubSub
    {
        #region GCP
        Task PublishGCP(IntegrationEvent @event, string topicId);

        void SubscribeGCP();

        Task SubscriberCreateGCP(string subscriber);

        #endregion
    }
}
