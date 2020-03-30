using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Handlers;
using WorkerService.Messages;

namespace WebApplication
{
    public class SaySomethingResponseHandler : IHandleMessages<SaySomethingResponse>
    {
        private readonly ILogger<SaySomethingResponseHandler> _logger;
        private IBus _bus;
        public SaySomethingResponseHandler(ILogger<SaySomethingResponseHandler> logger, IBus bus)
        {
            _logger = logger;
            _bus = bus;
        }

        public Task Handle(SaySomethingResponse message)
        {
            _logger.LogInformation("Received {message}", message.Message);

            return Task.CompletedTask;
        }
    }
}