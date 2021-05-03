using MessageBusCore.Abstractions;
using MessageBusCore.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PubSubApi.Infrastructure.Models.IntegrationEvents;
using PubSubApi.Infrastructure.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PubSubApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PubSubController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        public PubSubController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        [HttpGet("/Default")]
        public Task DefaultAction()
        {
            return Task.CompletedTask;
        }

        [HttpPost("/Publish/Message")]
        public async Task<IActionResult> Publish([FromBody] PublishMessage message)
        {
            if (ModelState.IsValid)
            {
                if (message.IsPublish ?? false)
                    await Task.Run(() => _eventBus.PublishAzure(new PublishMessageEvent(message.Message, message.PublishedAt)));

                return Ok();
            }
            else
            {
                return Ok("Model-State Invalid");
            }
        }

        [HttpPost("/Subscribe/Message")]
        public async Task<IActionResult> Subscribe([FromQuery, BindRequired] string subscriberName)
        {
            await Task.Run(() => _eventBus.SubscriberCreateAzure<PublishMessageEvent, PublishMessageEventHandler>(subscriberName));
            return Ok("Model-State Invalid");
        }
    }
}
