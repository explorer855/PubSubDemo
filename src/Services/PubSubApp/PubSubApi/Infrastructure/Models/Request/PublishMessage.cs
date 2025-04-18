﻿using System;
using System.ComponentModel.DataAnnotations;

namespace PubSubApi.Infrastructure.Models.Request
{
    public class PublishMessage
    {
        [Required]
        public bool? IsPublish { get; set; }

        [Required]
        public dynamic MessageContent { get; set; }
        public DateTime? PublishedAt { get; set; } = DateTime.UtcNow;
    }

    public class PublishMessageGcp :
        PublishMessage
    {
        public string Topic { get; set; }
    }
}
