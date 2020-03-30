namespace Rebus.OpenTelemetry
{
    internal static class Constants
    {
        internal const string TraceParentHeaderName = "rbs-ot-traceparent";
        internal const string TraceStateHeaderName = "rbs-ot-tracestate";
        internal const string CorrelationContextHeaderName = "rbs-ot-correlation-context";
        internal const string RequestIdHeaderName = "rbs-ot-request-id";

        internal const string ConsumerActivityName = "Rebus.Diagnostics.Receive";
        internal const string ProducerActivityName = "Rebus.Diagnostics.Send";
    }
}

