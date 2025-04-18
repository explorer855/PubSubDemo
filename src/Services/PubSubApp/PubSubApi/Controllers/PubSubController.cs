﻿using MessageBusCore.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PubSubApi.Infrastructure.IntegrationEvents;
using System;
using System.Threading.Tasks;

namespace PubSubApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PubSubController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly IGcpPubSub _pubSub;
        public PubSubController(IEventBus eventBus,
            IGcpPubSub gcpPubSub)
        {
            _eventBus = eventBus;
            _pubSub = gcpPubSub;
        }

        [HttpGet("/Default")]
        public async Task<IActionResult> DefaultAction()
        {
            await Task.CompletedTask;
            return Ok($"OK: {DateTime.Now.ToLongTimeString()}");
        }

        /// <summary>
        /// Publish messages to Azure Service Bus Topic
        /// </summary>
        /// <param name="message"></param>
        [HttpPost("/Publish/Message/Az")]
        public async Task<IActionResult> Publish([FromBody] MessageObjectEntity message)
        {
            if (ModelState.IsValid)
            {
                await _eventBus.PublishAzure(new PublishMessageEvent(message.MessageContent, message.TimeStamp), message.Topic);
                return Ok();
            }
            else
            {
                return Ok("Model-State Invalid");
            }
        }

        /// <summary>
        /// Subscribe to messages of Azure Service Bus Topic
        /// </summary>
        /// <param name="subscriberName"></param>
        [HttpPost("/Subscribe/Message/Az")]
        public async Task<IActionResult> Subscribe([FromQuery, BindRequired] string topic, [FromQuery, BindRequired] string subscriberName)
        {
            await _eventBus.SubscribeAzure<PublishMessageEvent, ServiceBusMessageEventHandler>(subscriberName, topic);
            return Ok();
        }

        /// <summary>
        /// Subscribe to messages of Azure Service Bus Topic
        /// </summary>
        /// <param name="subscriberName"></param>
        [HttpPost("/SubscribeWithSEssion/Message/Az")]
        public async Task<IActionResult> SubscribeWOSession([FromQuery, BindRequired] string topic, [FromQuery, BindRequired] string subscriberName)
        {
            await _eventBus.SubscriberCreateAzure<PublishMessageEvent, ServiceBusMessageEventHandler>(subscriberName, topic);
            return Ok();
        }

        /// <summary>
        /// Publish Messages to GCP Pub/Sub Topic
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost("/Publish/Message/Gcp")]
        public async Task<IActionResult> PublishGcp([FromBody] MessageObjectEntity message)
        {
            if (ModelState.IsValid)
            {
                await _pubSub.PublishGCP(new PublishMessageEvent(message.MessageContent, message.TimeStamp), message.Topic);
                return Ok();
            }
            else
            {
                return Ok("Model-State Invalid");
            }
        }

        /// <summary>
        /// Subscribe Messages from GCP Pub/Sub Topic
        /// </summary>
        /// <param name="subscriberName"></param>
        /// <returns></returns>
        [HttpPost("/Subscribe/Message/Gcp")]
        public async Task<IActionResult> SubscribeGcp([FromQuery, BindRequired] string subscriberName)
        {
            await _pubSub.SubscriberCreateGCP<PublishMessageEvent, PubSubMessageEventHandler>(subscriberName);
            return Ok();
        }

        /// <summary>
        /// Api to list all SQS Queues
        /// </summary>
        /// <returns></returns>
        [HttpGet("/Queues/List/Sqs")]
        public async Task<IActionResult> SqsQueues()
        {
            //await _awsSqs.ShowQueues();
            return Ok();
        }

        /// <summary>
        /// Publish Messages to AWS SQS Topic
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost("/Publish/Message/Sqs")]
        public async Task<IActionResult> PublishSqs([FromBody] MessageObjectEntity message)
        {
            if (ModelState.IsValid)
            {
                //await _awsSqs.PublishSqs(new PublishMessageEvent(message.MessageContent, message.TimeStamp));
                return Ok();
            }
            else
            {
                return Ok("Model-State Invalid");
            }
        }

        /// <summary>
        /// Subscribe Messages from AWS SQS Topic
        /// </summary>
        /// <param name="subscriberName"></param>
        /// <returns></returns>
        [HttpPost("/Subscribe/Message/Sqs")]
        public async Task<IActionResult> SubscribeSqs([FromQuery, BindRequired] string subscriberName)
        {
            //await _awsSqs.SubscriberCreateSqs<PublishMessageEvent, SqsMessageEventHandler>(subscriberName);
            return Ok();
        }
    }
}
