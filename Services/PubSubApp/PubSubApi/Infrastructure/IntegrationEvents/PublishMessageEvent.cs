using MessageBusCore.Events;
using System;

namespace PubSubApi.Infrastructure.IntegrationEvents
{
    public class PublishMessageEvent
        : IntegrationEvent
    {

        public PublishMessageEvent(dynamic msg, DateTime? time)
        {
            Message = msg;
            PublishedAt = time;
        }

        public dynamic Message { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
