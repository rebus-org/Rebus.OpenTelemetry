using System.Diagnostics;

namespace Rebus.Diagnostics
{
    public static class RebusDiagnosticConstants
    {
        public const string TraceStateHeaderName = "rbs-ot-tracestate";
        public const string BaggageHeaderName = "rbs-ot-correlation-context";
        

        public const string ConsumerActivityName = ActivitySourceName + ".Receive";
        public const string ProducerActivityName = ActivitySourceName + ".Send";

        public const string ActivitySourceName = "Rebus.Diagnostics";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName,
            typeof(RebusDiagnosticConstants).Assembly.GetName().Version.ToString());
    }
}

