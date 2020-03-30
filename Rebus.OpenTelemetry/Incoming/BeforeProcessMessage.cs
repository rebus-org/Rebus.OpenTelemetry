using System;
using System.Diagnostics;
using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Incoming
{
    public class BeforeProcessMessage
    {
        public const string EventName = Constants.ConsumerActivityName + "." + nameof(BeforeProcessMessage);

        public BeforeProcessMessage(IncomingStepContext context, Activity activity)
        {
            Context = context;
            StartTimeUtc = activity.StartTimeUtc;
        }

        public IncomingStepContext Context { get; }
        
        public DateTime StartTimeUtc { get; }
    }
}