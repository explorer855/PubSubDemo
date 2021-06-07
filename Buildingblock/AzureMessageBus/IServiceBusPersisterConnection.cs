using Microsoft.Azure.ServiceBus;

namespace AzureMessageBus
{
    public interface IServiceBusPersisterConnection
    {
        ITopicClient TopicClient(string topicName);
        ISubscriptionClient SubscriptionClient { get; }

        ISubscriptionClient SubscriptionClientCreate(string subscriber, string topicName);
    }
}
