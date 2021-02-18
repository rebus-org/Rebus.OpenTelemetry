using System.Diagnostics;

namespace Rebus.Diagnostics
{
    public static class Constants
    {
        public const string TraceStateHeaderName = "rbs-ot-tracestate";

        public const string ConsumerActivityName = ActivitySourceName + ".Receive";
        public const string ProducerActivityName = ActivitySourceName + ".Send";

        public const string ActivitySourceName = "Rebus.Diagnostics";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName,
            typeof(Constants).Assembly.GetName().Version.ToString());
    }
}

