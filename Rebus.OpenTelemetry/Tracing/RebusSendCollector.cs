using System;
using OpenTelemetry.Collector;
using OpenTelemetry.Trace;
using Rebus.OpenTelemetry.Listeners;

namespace Rebus.OpenTelemetry.Tracing
{
    public class RebusSendCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber _diagnosticSourceSubscriber;

        public RebusSendCollector(Tracer tracer)
        {
            _diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new SendMessageListener(Constants.ProducerActivityName, tracer), null);
            _diagnosticSourceSubscriber.Subscribe();
        }


        public void Dispose() => _diagnosticSourceSubscriber.Dispose();
    }
}