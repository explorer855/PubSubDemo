using Newtonsoft.Json;
using System;

namespace MessageBusCore.Events
{
    public class IntegrationEvent
    {
        public IntegrationEvent()
        {
            Id = Guid.NewGuid();
            CreatedOn = DateTime.UtcNow;
        }

        [JsonConstructor]
        public IntegrationEvent(Guid id, DateTime createdOn)
        {
            Id = id;
            CreatedOn = createdOn;
        }

        [JsonProperty]
        public Guid Id { get; private set; }

        [JsonProperty]
        public DateTime CreatedOn { get; private set; }
    }
}