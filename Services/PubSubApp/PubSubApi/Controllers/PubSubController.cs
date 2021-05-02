using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public PubSubController()
        {

        }

        [HttpGet("/Default")]
        public Task DefaultAction()
        {
            return Task.CompletedTask;
        }
    }
}
