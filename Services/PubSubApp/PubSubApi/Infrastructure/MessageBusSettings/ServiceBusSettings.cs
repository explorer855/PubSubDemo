using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PubSubApi.Infrastructure.MessageBusSettings
{
    public sealed class ServiceBusSettings
    {
        public string ConnectionString { get; set; }

        public string Topicwithoutsession { get; set; }

        public string Subscriberwithoutsession { get; set; }

        public string Topicwithsession { get; set; }

        public string Subscriberwithsession { get; set; }
    }
}
