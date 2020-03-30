using System;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;
using Rebus.OpenTelemetry.Listeners;

namespace Rebus.OpenTelemetry.Tracing
{
    public class RebusReceiveCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RebusReceiveCollector(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new ProcessMessageListener(Constants.ConsumerActivityName, tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }


        public void Dispose() => _diagnosticSourceSubscriber.Dispose();
    }
}
