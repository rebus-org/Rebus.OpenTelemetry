using Rebus.Pipeline;

namespace Rebus.Diagnostics.Outgoing
{
    public class AfterSendMessage
    {
        public const string EventName = RebusDiagnosticConstants.ProducerActivityName + "." + nameof(AfterSendMessage);

        public AfterSendMessage(OutgoingStepContext context) => Context = context;

        public OutgoingStepContext Context { get; }
    }
}