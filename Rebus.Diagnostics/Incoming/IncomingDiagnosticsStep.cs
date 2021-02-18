using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Rebus.Bus;
using Rebus.Diagnostics.Helpers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Diagnostics.Incoming
{
    [StepDocumentation("Extracts trace from the incoming message and starts an activity for it")]
    public class IncomingDiagnosticsStep : IIncomingStep
    {
        private static readonly DiagnosticSource DiagnosticListener =
            new DiagnosticListener(Constants.ConsumerActivityName);

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            using var activity = StartActivity(context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private static Activity? StartActivity(IncomingStepContext context)
        {
            Activity? activity = null;
            if (Constants.ActivitySource.HasListeners())
            {
                var message = context.Load<TransportMessage>();

                var headers = message.Headers;

                if (!headers.TryGetValue(Constants.TraceStateHeaderName, out var traceState))
                {
                    return null;
                }

                var messageType = message.GetMessageType();

                var messageWrapper = new TransportMessageWrapper(message);
            
                var initialTags = TagHelper.ExtractInitialTags(messageWrapper);
                initialTags.Add("messaging.operation", "receive");

                var activityKind = messageWrapper.GetIntentOption() == Headers.IntentOptions.PublishSubscribe
                    ? ActivityKind.Consumer
                    : ActivityKind.Server;

                activity = Constants.ActivitySource.StartActivity($"{messageType} receive", activityKind, traceState, initialTags);

                // TODO: Not sure if this is still needed
                // DiagnosticListener.OnActivityImport(activity, context);
            }
            
            if (DiagnosticListener.IsEnabled(BeforeProcessMessage.EventName, context))
            {
                DiagnosticListener.Write(BeforeProcessMessage.EventName, new BeforeProcessMessage(context, activity));
            }

            return activity;
        }

        private static void StopActivity(Activity? activity, IncomingStepContext context)
        {
            if (DiagnosticListener.IsEnabled(AfterProcessMessage.EventName))
            {
                DiagnosticListener.Write(AfterProcessMessage.EventName, new AfterProcessMessage(context, activity));
            }
        }
    }
}