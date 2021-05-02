using Microsoft.Azure.ServiceBus;

namespace AzureMessageBus
{
    public interface IServiceBusPersisterConnection
    {
        ITopicClient TopicClient { get; }
        ISubscriptionClient SubscriptionClient { get; }

        ISubscriptionClient SubscriptionClientCreate(string subscriber);
    }
}
