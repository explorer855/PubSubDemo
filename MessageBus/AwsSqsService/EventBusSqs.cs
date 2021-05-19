using Amazon.SQS;
using Amazon.SQS.Model;
using Autofac;
using MessageBusCore;
using MessageBusCore.Abstractions;
using MessageBusCore.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsSqsService
{
    public sealed class EventBusSqs
        : IAwsSqsQueue
    {
        private readonly ISqsPersisterConnection _serviceBusPersisterConnection;
        private readonly ILogger<EventBusSqs> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME;
        private const string INTEGRATION_EVENT_SUFFIX = "IntegrationEvent";
        private string _subscriber;
        private readonly IAmazonSQS _sqs;

        public EventBusSqs(ISqsPersisterConnection serviceBusPersisterConnection,
            ILogger<EventBusSqs> logger, IEventBusSubscriptionsManager subsManager,
            ILifetimeScope autofac, IConfiguration configuration, IAmazonSQS amazonSQS)
        {
            AUTOFAC_SCOPE_NAME = configuration.GetSection("NameSpace:AWS")?.Value.ToString();
            _serviceBusPersisterConnection = serviceBusPersisterConnection;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            _autofac = autofac;
            _subscriber = string.Empty;
            _sqs = amazonSQS;
        }

        public async Task PublishSqs(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
            var jsonMessage = JsonConvert.SerializeObject(@event);

            var responseList = await _sqs.ListQueuesAsync("");
            var objMessage = new SendMessageRequest
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {
                      "EventName", new MessageAttributeValue
                        { DataType = "String", StringValue = eventName }
                    },
                },
                MessageBody = jsonMessage,
                QueueUrl = responseList.QueueUrls?.FirstOrDefault()
            };
            await _sqs.SendMessageAsync(objMessage);
        }

        public void SubscribeDynamicSqs<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public async Task SubscribeSqs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");

            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
            if (!containsKey)
            {
                
            }

            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

            _subsManager.AddSubscription<T, TH>();
        }

        public async Task SubscriberCreateSqs<T, TH>(string subscriber)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
            _subscriber = subscriber;
            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
           

            if (!containsKey)
            {
                _subsManager.AddSubscription<T, TH>();
                var responseList = await _sqs.ListQueuesAsync("");

                var objMessages = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = responseList?.QueueUrls.FirstOrDefault(),
                    WaitTimeSeconds = 10,
                    AttributeNames = new List<string>() { "All" },
                    MaxNumberOfMessages = 10
                });

                if (objMessages?.Messages.Count > 0)
                {
                    _subsManager.AddSubscription<T, TH>();
                    objMessages.Messages.ForEach(async x =>
                    {
                        if(await ProcessEvent(eventName, x.Body))
                            await _sqs.DeleteMessageAsync(responseList?.QueueUrls.FirstOrDefault(), x.ReceiptHandle);
                    });
                }
            }
            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
            _subsManager.AddSubscription<T, TH>();
        }

        public void UnsubscribeSqs<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamicSqs<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Unsubscribing from dynamic event {EventName}", eventName);

            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }
        public async Task ShowQueues()
        {
            ListQueuesResponse responseList = await _sqs.ListQueuesAsync("");
            Console.WriteLine();
            foreach (string qUrl in responseList.QueueUrls)
            {
                // Get and show all attributes. Could also get a subset.
                await ShowAllAttributes(_sqs, qUrl);
            }
        }

        private static async Task ShowAllAttributes(IAmazonSQS sqsClient, string qUrl)
        {
            var attributes = new List<string> { QueueAttributeName.All };
            GetQueueAttributesResponse responseGetAtt =
              await sqsClient.GetQueueAttributesAsync(qUrl, attributes);
            Console.WriteLine($"Queue: {qUrl}");
            foreach (var att in responseGetAtt.Attributes)
                Console.WriteLine($"\t{att.Key}: {att.Value}");
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

        public void Dispose()
        {
            _subsManager.Clear();
        }
    }
}
