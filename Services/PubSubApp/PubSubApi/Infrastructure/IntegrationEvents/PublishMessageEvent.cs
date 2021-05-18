using MessageBusCore.Events;
using System;
using System.ComponentModel.DataAnnotations;

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

    public class MessageObjectEntity
    {
        public string SessionId { get; set; }

        [Required]
        public string PrimaryKey { get; set; }

        [Required]
        public dynamic MessageContent { get; set; }

        public string Topic { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }
}
