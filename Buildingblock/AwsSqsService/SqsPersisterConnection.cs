using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AwsSqsService
{
    public sealed class SqsPersisterConnection
        : ISqsPersisterConnection, IDisposable 
    {
        bool _disposed;

        public SqsPersisterConnection(ILogger<SqsPersisterConnection> logger,
            IConfiguration configuration)
        {
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
        }
    }
}
