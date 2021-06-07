using Google.Cloud.PubSub.V1;
using System.Threading.Tasks;

namespace GooglePubSub
{
    public interface IPubSubPersisterConnection
    {
        Task<PublisherClient> PublisherClientAsync(string topicId);
        Task<SubscriberClient> SubscriberClientAsync(string subscriptionId);
    }
}
