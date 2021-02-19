using Rebus.Pipeline;

namespace Rebus.Diagnostics.Outgoing
{
    public class BeforeSendMessage
    {
        public const string EventName = RebusDiagnosticConstants.ProducerActivityName + "." + nameof(BeforeSendMessage);

        public BeforeSendMessage(OutgoingStepContext context) => Context = context;

        public OutgoingStepContext Context { get; }
    }
}