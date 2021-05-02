﻿using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using System;

namespace AzureMessageBus
{
    public sealed class ServiceBusPersisterConnection
        : IServiceBusPersisterConnection
    {
        private readonly ILogger<ServiceBusPersisterConnection> _logger;
        private readonly ServiceBusConnectionStringBuilder _serviceBusConnectionStringBuilder;
        private readonly string _subscriptionClientName;
        private SubscriptionClient _subscriptionClient;
        private ITopicClient _topicClient;

        bool _disposed;

        public ServiceBusPersisterConnection(ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder,
            ILogger<ServiceBusPersisterConnection> logger, string subscriptionClientName = "")
        {
            _serviceBusConnectionStringBuilder = serviceBusConnectionStringBuilder ??
               throw new ArgumentNullException(nameof(serviceBusConnectionStringBuilder));
            _subscriptionClientName = subscriptionClientName;
            //_serviceBusConnectionStringBuilder.EntityPath = "topic1";

            //if (!string.IsNullOrEmpty(subscriptionClientName))
            //    _subscriptionClient = new SubscriptionClient(_serviceBusConnectionStringBuilder, subscriptionClientName);

            //_topicClient = new TopicClient(_serviceBusConnectionStringBuilder, RetryPolicy.Default);
        }

        public ITopicClient TopicClient
        {
            get
            {
                if (_topicClient?.IsClosedOrClosing ?? false || _topicClient == null)
                {
                    _topicClient = new TopicClient(_serviceBusConnectionStringBuilder, RetryPolicy.Default);
                }
                return _topicClient;
            }
        }

        public ISubscriptionClient SubscriptionClient
        {
            get
            {
                if (_subscriptionClient?.IsClosedOrClosing ?? false || _subscriptionClient == null)
                {
                    _subscriptionClient = new SubscriptionClient(_serviceBusConnectionStringBuilder, _subscriptionClientName);
                }
                return _subscriptionClient;
            }
        }

        public ServiceBusConnectionStringBuilder ServiceBusConnectionStringBuilder => _serviceBusConnectionStringBuilder;

        public ITopicClient CreateModel()
        {
            if (_topicClient?.IsClosedOrClosing ?? false || _topicClient == null)
            {
                _topicClient = new TopicClient(_serviceBusConnectionStringBuilder, RetryPolicy.Default);
            }

            return _topicClient;
        }

        public ISubscriptionClient SubscriptionClientCreate(string subscriber)
        {
            try
            {
                if (_subscriptionClient?.IsClosedOrClosing ?? false || _subscriptionClient == null)
                {
                    _subscriptionClient = new SubscriptionClient(_serviceBusConnectionStringBuilder, subscriber);
                }
                return _subscriptionClient;
            }
            catch
            {
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}
