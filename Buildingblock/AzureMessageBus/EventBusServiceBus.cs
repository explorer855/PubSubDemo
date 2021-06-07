using Autofac;
using MessageBusCore;
using MessageBusCore.Abstractions;
using MessageBusCore.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureMessageBus
{
    public sealed class EventBusServiceBus
        : IEventBus, IDisposable
    {
        private readonly IServiceBusPersisterConnection _serviceBusPersisterConnection;
        private readonly ILogger<EventBusServiceBus> _logger;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;
        private readonly string AUTOFAC_SCOPE_NAME;
        private const string INTEGRATION_EVENT_SUFFIX = "IntegrationEvent";
        private string _subscriber;
        private string _topic;

        public EventBusServiceBus(IServiceBusPersisterConnection serviceBusPersisterConnection,
            ILogger<EventBusServiceBus> logger, IEventBusSubscriptionsManager subsManager, string subscriptionClientName,
            ILifetimeScope autofac, IConfiguration configuration)
        {
            _serviceBusPersisterConnection = serviceBusPersisterConnection;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
            AUTOFAC_SCOPE_NAME = configuration.GetSection("NameSpace:Azure")?.Value.ToString();
            _autofac = autofac;
            _subscriber = string.Empty;
            _topic = string.Empty;
        }

        public async Task PublishAzure(IntegrationEvent @event, string topicName)
        {
            var eventName = @event.GetType().Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
            var jsonMessage = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                SessionId = Guid.NewGuid().ToString(),
                Body = body,
                Label = eventName,
            };
            await _serviceBusPersisterConnection.TopicClient(topicName).SendAsync(message);
        }

        public void SubscribeDynamicAzure<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Subscribing to dynamic event {EventName} with {EventHandler}", eventName, typeof(TH).Name);

            _subsManager.AddDynamicSubscription<TH>(eventName);
        }

        public async Task SubscribeAzure<T, TH>(string subscriber, string topic)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");

            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
            try
                {
                    await _serviceBusPersisterConnection.SubscriptionClientCreate(subscriber, topic).AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter { Label = eventName },
                        Name = eventName,
                    });
                }
                catch (ServiceBusException)
                {
                    _logger.LogWarning("The messaging entity {eventName} already exists.", eventName);
                }
                finally
                {
                    RemoveDefaultRule(subscriber, topic);

                    if (!containsKey)
                        _subsManager.AddSubscription<T, TH>();

                    RegisterSubscriptionClientMessageHandler(subscriber, topic);
                }
            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        }

        public async Task SubscriberCreateAzure<T, TH>(string subscriber, string topic)
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");
            _subscriber = subscriber;
            var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
                try
                {
                    await _serviceBusPersisterConnection.SubscriptionClientCreate(subscriber, topic).AddRuleAsync(new RuleDescription
                    {
                        Filter = new CorrelationFilter { Label = eventName },
                        Name = eventName,
                    });
                }
                catch (ServiceBusException)
                {
                    _logger.LogWarning("The Rules to create filters for the messaging entity {eventName} already exists.", eventName);
                    throw;
                }
                finally
                {
                    RemoveDefaultRule(subscriber, topic);
                    if (!containsKey)
                        _subsManager.AddSubscription<T, TH>();

                    RegisterSessionEnabledSubscriptionClientMessageHandler(subscriber, topic);
                }
            _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, typeof(TH).Name);
        }

        public void UnsubscribeAzure<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(INTEGRATION_EVENT_SUFFIX, "");

            try
            {
                _serviceBusPersisterConnection
                    .SubscriptionClient
                    .RemoveRuleAsync(eventName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {eventName} Could not be found.", eventName);
            }

            _logger.LogInformation("Unsubscribing from event {EventName}", eventName);

            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamicAzure<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _logger.LogInformation("Unsubscribing from dynamic event {EventName}", eventName);

            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        private void RegisterSubscriptionClientMessageHandler(string subscriber, string topic)
        {
            _serviceBusPersisterConnection.SubscriptionClientCreate(subscriber, topic)?.RegisterMessageHandler(
           async (message, token) =>
           {
               var messageData = Encoding.UTF8.GetString(message.Body);
               if (await ProcessEvent(message.Label, messageData))
               {
                   await _serviceBusPersisterConnection.SubscriptionClientCreate(subscriber, topic).CompleteAsync(message.SystemProperties.LockToken);
               }
           },
           new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });
        }

        private void RegisterSessionEnabledSubscriptionClientMessageHandler(string subscriber, string topicName)
        {
            _topic = topicName;
            var sessionHandlerOptions = new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentSessions = 2,
                MessageWaitTimeout = TimeSpan.FromSeconds(1),
                AutoComplete = false
            };
            _serviceBusPersisterConnection.SubscriptionClientCreate(subscriber, topicName)?.RegisterSessionHandler(ProcessSessionMessagesAsync, sessionHandlerOptions);
        }

        private async Task ProcessSessionMessagesAsync(IMessageSession session, Message message, CancellationToken token)
        {
            Console.WriteLine($"Received Session: {session.SessionId} message: SequenceNumber: {message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");
            var messageData = Encoding.UTF8.GetString(message.Body);
            if (await ProcessEvent(message.Label, messageData))
            {
                await session.CompleteAsync(message.SystemProperties.LockToken);
                await _serviceBusPersisterConnection.SubscriptionClientCreate(_subscriber, _topic).CompleteAsync(message.SystemProperties.LockToken);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

            return Task.CompletedTask;
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

        private void RemoveDefaultRule(string subscriber, string topic)
        {
            try
            {
                _serviceBusPersisterConnection
               .SubscriptionClientCreate(subscriber, topic)?
               .RemoveRuleAsync(RuleDescription.DefaultRuleName)
                .GetAwaiter()
                .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {DefaultRuleName} Could not be found.", RuleDescription.DefaultRuleName);
            }
        }

        public void Dispose()
        {
            _subsManager.Clear();
        }
    }
}
