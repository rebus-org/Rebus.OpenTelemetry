using System;
using System.Diagnostics;
using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Incoming
{
    public class AfterProcessMessage
    {
        public const string EventName = Constants.ConsumerActivityName + "." + nameof(AfterProcessMessage);

        public AfterProcessMessage(IncomingStepContext context, Activity activity)
        {
            Context = context;
            StartTimeUtc = activity.StartTimeUtc;
            Duration = activity.Duration;
        }

        
        public IncomingStepContext Context { get; }
        
        public DateTime StartTimeUtc { get; }
        
        public TimeSpan Duration { get; }
    }
}