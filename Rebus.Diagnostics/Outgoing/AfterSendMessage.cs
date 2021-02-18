using Rebus.Pipeline;

namespace Rebus.Diagnostics.Outgoing
{
    public class AfterSendMessage
    {
        public const string EventName = Constants.ProducerActivityName + "." + nameof(AfterSendMessage);

        public AfterSendMessage(OutgoingStepContext context) => Context = context;

        public OutgoingStepContext Context { get; }
    }
}