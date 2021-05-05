using Google.Apis.Auth.OAuth2;
using Google.Cloud.PubSub.V1;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace GooglePubSub
{
    /// <summary>
    /// Class handles Creating Publishing, Subscribing Clients
    /// </summary>
    public sealed class PubSubPersisterConnection
        : IPubSubPersisterConnection
    {
        bool _disposed;
        private readonly IConfiguration _config;
        public PubSubPersisterConnection(IConfiguration configuration)
        {
            _config = configuration;
        }

        public async Task<PublisherClient> PublisherClientAsync(string topicId)
        {
            try
            {
                //AuthImplicit();
                TopicName topicName = TopicCreate(topicId);
                PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
                return publisher;
            }
            catch
            {
                throw;
            }
        }

        public async Task<SubscriberClient> SubscriberClientAsync(string subscriptionId)
        {
            try
            {
                SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(_config.GetSection("GooglePubSubSettings:ProjectID")?.Value, subscriptionId);
                SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
                return subscriber;
            }
            catch
            {
                throw;
            }
        }

        private TopicName TopicCreate(string topicId)
        {
            try
            {
                return new TopicName(_config.GetSection("GooglePubSubSettings:ProjectID")?.Value, topicId);
            }
            catch
            {
                throw;
            }
        }

        public void AuthImplicit()
        {
            // If you don't specify credentials when constructing the client, the
            // client library will look for credentials in the environment.
            var credential = GoogleCredential.GetApplicationDefault();
            var storage = StorageClient.Create(credential);
            // Make an authenticated API request.
            var buckets = storage.ListBuckets(_config.GetSection("GooglePubSubSettings:ProjectID")?.Value);
            foreach (var bucket in buckets)
            {
                Console.WriteLine(bucket.Name);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}
