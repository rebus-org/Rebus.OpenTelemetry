using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Outgoing
{
    public class AfterSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + "." + nameof(AfterSendMessage);

        public AfterSendMessage(OutgoingStepContext context) => Context = context;

        public OutgoingStepContext Context { get; }
    }
}