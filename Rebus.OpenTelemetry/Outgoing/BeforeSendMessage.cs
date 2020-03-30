using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Outgoing
{
    public class BeforeSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + "." + nameof(BeforeSendMessage);

        public BeforeSendMessage(OutgoingStepContext context) => Context = context;

        public OutgoingStepContext Context { get; }
    }
}