using MessageBusCore.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PubSubApi.Infrastructure.Models.IntegrationEvents
{
    public class PublishMessageEventHandler 
        : IIntegrationEventHandler<PublishMessageEvent>
    {
        private readonly ILogger<PublishMessageEventHandler> _logger;

        public PublishMessageEventHandler(ILogger<PublishMessageEventHandler> logger)
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
