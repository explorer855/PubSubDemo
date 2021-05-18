using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwsSqsService
{
    public sealed class SqsPersisterConnection
        : ISqsPersisterConnection 
    {
        private readonly ILogger<SqsPersisterConnection> _logger;
        private readonly IConfiguration _config;
        private readonly string _subscriptionClientName;

        bool _disposed;

        public SqsPersisterConnection(ILogger<SqsPersisterConnection> logger,
            IConfiguration configuration)
        {
            _config = configuration;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}
