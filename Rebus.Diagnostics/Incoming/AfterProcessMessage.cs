using System;
using System.Diagnostics;
using Rebus.Pipeline;

namespace Rebus.Diagnostics.Incoming
{
    public class AfterProcessMessage
    {
        public const string EventName = Constants.ConsumerActivityName + "." + nameof(AfterProcessMessage);

        public AfterProcessMessage(IncomingStepContext context, Activity? activity)
        {
            Context = context;
            StartTimeUtc = activity?.StartTimeUtc ?? default;
            Duration = activity?.Duration ?? default;
        }

        
        public IncomingStepContext Context { get; }
        
        public DateTime StartTimeUtc { get; }
        
        public TimeSpan Duration { get; }
    }
}