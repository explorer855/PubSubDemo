using MessageBusCore.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PubSubApi.Infrastructure.IntegrationEvents
{
    public class ServiceBusMessageEventHandler 
        : IIntegrationEventHandler<PublishMessageEvent>
    {
        private readonly ILogger<ServiceBusMessageEventHandler> _logger;

        public ServiceBusMessageEventHandler(ILogger<ServiceBusMessageEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(PublishMessageEvent @event)
        {
            try
            {
                _logger.LogInformation("Event Consumption Started at {Time}, {EventCreated}", DateTime.UtcNow, @event.CreatedOn);
                _logger.LogInformation("Event Ended at {Time}", DateTime.UtcNow);
                return Task.CompletedTask;
            }
            catch
            {
                _logger.LogError("Error caused at {Time}", DateTime.UtcNow);
                throw;
            }
        }
    }
}
