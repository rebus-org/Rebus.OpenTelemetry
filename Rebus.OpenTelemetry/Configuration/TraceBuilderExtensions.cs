using System;
using OpenTelemetry.Trace.Configuration;
using Rebus.Config;
using Rebus.OpenTelemetry.Incoming;
using Rebus.OpenTelemetry.Outgoing;
using Rebus.OpenTelemetry.Tracing;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.OpenTelemetry.Configuration
{
    public static class TraceBuilderExtensions
    {
        public static void EnableOpenTelemetry(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var step = new OutgoingDiagnosticsStep();
                return new PipelineStepInjector(pipeline).OnSend(step, PipelineRelativePosition.Before,
                    typeof(SerializeOutgoingMessageStep));
            });

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var step = new IncomingDiagnosticsStep();
                return new PipelineStepInjector(pipeline).OnReceive(step, PipelineRelativePosition.Before,
                    typeof(DeserializeIncomingMessageStep));
            });
        }

        public static TracerBuilder AddRebusCollectors(this TracerBuilder builder) =>
            builder
                .AddCollector(tracer => new RebusReceiveCollector(tracer))
                .AddCollector(tracer => new RebusSendCollector(tracer));
    }
}
