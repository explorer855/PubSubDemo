using Autofac;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using MessageBusCore;
using MessageBusCore.Abstractions;
using MessageBusCore.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GooglePubSub
{
    public sealed class EventBusPubSub
        : IGcpPubSub
    {
        private readonly ILogger<EventBusPubSub> _logger;
        private readonly IPubSubPersisterConnection _pubSubConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME = "event_bus";
        private const string INTEGRATION_EVENT_SUFFIX = "IntegrationEvent";

        public EventBusPubSub(IPubSubPersisterConnection pubSubPersister, IEventBusSubscriptionsManager subsManager, ILifetimeScope lifetimeScope,
            ILogger<EventBusPubSub> logger)
        {
            _pubSubConnection = pubSubPersister;
            _subsManager = subsManager;
            _autofac = lifetimeScope;
            _logger = logger;
        }

        #region GCP

        public async Task PublishGCP(IntegrationEvent @event, string topicId)
        {
            try
            {
                var publisher = await _pubSubConnection.PublisherClientAsync(topicId);
                var jsonMessage = JsonConvert.SerializeObject(@event);
                var eventName = @event.GetType().Name.Replace(INTEGRATION_EVENT_SUFFIX, "");

                await publisher.PublishAsync(PubSubMessage(jsonMessage, eventName));
            }
            catch
            {
                throw;
            }
        }

        public void SubscribeGCP<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new System.NotImplementedException();
        }

        public async Task SubscriberCreateGCP<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            try
            {
                var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
                var containsKey = _subsManager.HasSubscriptionsForEvent<T>();

                if (!containsKey)
                {
                    var _subscriber = await _pubSubConnection.SubscriberClientAsync(subscriber);
                    _subsManager.AddSubscription<T, TH>();
                    await ProcessMessagesAsync(_subscriber, eventName);
                    await _subscriber.StopAsync(CancellationToken.None);
                }
            }
            catch
            {
                throw;
            }
            _subsManager.AddSubscription<T, TH>();
        }

        private async Task ProcessMessagesAsync(SubscriberClient _subscriber, string eventName)
        {
            Task startTask = _subscriber.StartAsync(async (PubsubMessage message, CancellationToken cancel) =>
            {
                if(string.Equals(message.Attributes?.FirstOrDefault().Value, eventName))
                {
                    if (await ProcessEvent(eventName, System.Text.Encoding.UTF8.GetString(message.Data.ToArray())))
                        return SubscriberClient.Reply.Ack;
                    else
                        return SubscriberClient.Reply.Nack;
                }
                else
                    return SubscriberClient.Reply.Nack;
            });

            await startTask;
        }

        private async Task<bool> ProcessEvent(string eventName, string message)
        {
            var processed = false;
            if (_subsManager.HasSubscriptionsForEvent(eventName))
            {
                using (var scope = _autofac.BeginLifetimeScope(AUTOFAC_SCOPE_NAME))
                {
                    var subscriptions = _subsManager.GetHandlersForEvent(eventName);
                    foreach (var subscription in subscriptions)
                    {
                        if (subscription.IsDynamic)
                        {
                            if (scope.ResolveOptional(subscription.HandlerType) is not IDynamicIntegrationEventHandler handler) continue;
                            dynamic eventData = JObject.Parse(message);
                            await handler.Handle(eventData);
                        }
                        else
                        {
                            var handler = scope.ResolveOptional(subscription.HandlerType);
                            if (handler == null) continue;
                            var eventType = _subsManager.GetEventTypeByName(eventName);
                            var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                        }
                    }
                }
                processed = true;
            }
            return processed;
        }

        private static PubsubMessage PubSubMessage(string msgObject, string eventName)
        {
            return new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(msgObject),
                Attributes = { { "EventName", eventName } }
            };
        }

        #endregion
    }
}
