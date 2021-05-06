using MessageBusCore.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PubSubApi.Infrastructure.IntegrationEvents;
using PubSubApi.Infrastructure.Models.Request;
using System.Threading.Tasks;

namespace PubSubApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PubSubController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly IGcpPubSub _pubSub;
        public PubSubController(IEventBus eventBus, IGcpPubSub gcpPubSub)
        {
            _eventBus = eventBus;
            _pubSub = gcpPubSub;
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
                    await _eventBus.PublishAzure(new PublishMessageEvent(message.MessageContent, message.PublishedAt));

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
            await _eventBus.SubscriberCreateAzure<PublishMessageEvent, ServiceBusMessageEventHandler>(subscriberName);
            return Ok("Model-State Invalid");
        }

        [HttpPost("/Publish/Message/Gcp")]
        public async Task<IActionResult> PublishGcp([FromBody] PublishMessageGcp message)
        {
            if (ModelState.IsValid)
            {
                if (message.IsPublish ?? false)
                    await _pubSub.PublishGCP(new PublishMessageEvent(message.MessageContent, message.PublishedAt), message.Topic);

                return Ok();
            }
            else
            {
                return Ok("Model-State Invalid");
            }
        }

        [HttpPost("/Subscribe/Message/Gcp")]
        public async Task<IActionResult> SubscribeGcp([FromQuery, BindRequired] string subscriberName)
        {
            await _pubSub.SubscriberCreateGCP<PublishMessageEvent, PubSubMessageEventHandler>(subscriberName);
            return Ok();
        }
    }
}
