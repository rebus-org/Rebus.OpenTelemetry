using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using WorkerService.Messages;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SaySomethingController : ControllerBase
    {
        private readonly ILogger<SaySomethingController> _logger;
        private readonly IBus _messageSession;

        public SaySomethingController(ILogger<SaySomethingController> logger, IBus messageSession)
        {
            _logger = logger;
            _messageSession = messageSession;
        }

        [HttpGet]
        public async Task<ActionResult> Get(string message)
        {
            var command = new SaySomething
            {
                Message = message
            };

            _logger.LogInformation("Sending message {message}", command.Message);

            await _messageSession.Send(command);

            return Accepted();
        }

    }
}