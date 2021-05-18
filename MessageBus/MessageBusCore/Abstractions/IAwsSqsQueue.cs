using MessageBusCore.Events;
using System.Threading.Tasks;

namespace MessageBusCore.Abstractions
{
    public interface IAwsSqsQueue
    {
        Task PublishSqs(IntegrationEvent @event);
        void SubscribeDynamicSqs<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;
        Task SubscribeSqs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
        Task SubscriberCreateSqs<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
        void UnsubscribeSqs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
        void UnsubscribeDynamicSqs<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;
        Task ShowQueues();
    }
}
