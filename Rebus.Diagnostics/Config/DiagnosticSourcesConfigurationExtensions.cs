using System;
using Rebus.Diagnostics.Incoming;
using Rebus.Diagnostics.Outgoing;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.Config
{
    public static class DiagnosticSourcesConfigurationExtensions
    {
        public static void EnableDiagnosticSources(this OptionsConfigurer configurer)
        {
            if (configurer == null) throw new ArgumentNullException(nameof(configurer));

            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var injector = new PipelineStepInjector(pipeline);
                
                var outgoingStep = new OutgoingDiagnosticsStep();
                injector.OnSend(outgoingStep, PipelineRelativePosition.Before,
                    typeof(SendOutgoingMessageStep));
                
                var incomingStep = new IncomingDiagnosticsStep();
                injector.OnReceive(incomingStep, PipelineRelativePosition.Before,
                    typeof(DeserializeIncomingMessageStep));

                var invokerWrapper = new IncomingDiagnosticsHandlerInvokerWrapper();
                injector.OnReceive(invokerWrapper, PipelineRelativePosition.After, typeof(ActivateHandlersStep));
                
                return injector;
            });
        }
    }
}