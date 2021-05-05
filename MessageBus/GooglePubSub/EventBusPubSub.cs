using Autofac;
using Google.Cloud.PubSub.V1;
using MessageBusCore;
using MessageBusCore.Abstractions;
using MessageBusCore.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Text;
using Encoding = Google.Cloud.PubSub.V1.Encoding;
using System.Threading.Tasks;
using Google.Protobuf;

namespace GooglePubSub
{
    public sealed class EventBusPubSub
        : IGcpPubSub
    {
        private readonly ILogger<EventBusPubSub> _logger;
        private readonly IPubSubPersisterConnection _pubSubConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILifetimeScope _autofac;

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
                await publisher.PublishAsync(Message(jsonMessage));
            }
            catch
            {
                throw;
            }
        }

        public void SubscribeGCP()
        {
            throw new System.NotImplementedException();
        }

        public async Task SubscriberCreateGCP(string subscriber)
        {
            string messages = string.Empty;
            try
            {
                var _subscriber = await _pubSubConnection.SubscriberClientAsync(subscriber);
                Task startTask = _subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
                {
                    messages = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
                    return Task.FromResult(SubscriberClient.Reply.Ack);
                });

                await _subscriber.StopAsync(CancellationToken.None);
                await startTask;
                Console.WriteLine(messages);
            }
            catch
            {
                throw;
            }

        }

        private static PubsubMessage Message(string msgObject)
        {
            return new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(msgObject)
            };
        }

        #endregion
    }
}
