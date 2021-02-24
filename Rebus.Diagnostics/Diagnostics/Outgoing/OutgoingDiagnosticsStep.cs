using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Diagnostics.Helpers;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;

namespace Rebus.Diagnostics.Outgoing
{
    [StepDocumentation("Creates a new activity for sending the provided message and passes it along on the message")]
    public class OutgoingDiagnosticsStep : IOutgoingStep
    {
        private static readonly DiagnosticSource DiagnosticListener = new DiagnosticListener(RebusDiagnosticConstants.ProducerActivityName);

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            using var activity = StartActivity(context);
            
            InjectHeaders(activity, context);

            try
            {
                await next();
            }
            finally
            {
                SendAfterSendEvent(context);
            }
        }

        private static void InjectHeaders(Activity? activity, OutgoingStepContext context)
        {
            if (activity == null) return;
            
            var headers = context.Load<TransportMessage>().Headers;

            if (!headers.ContainsKey(RebusDiagnosticConstants.TraceStateHeaderName))
            {
                headers[RebusDiagnosticConstants.TraceStateHeaderName] = activity.Id;
            }

            if (!headers.ContainsKey(RebusDiagnosticConstants.BaggageHeaderName))
            {
                headers[RebusDiagnosticConstants.BaggageHeaderName] = JsonConvert.SerializeObject(activity.Baggage);
            }
        }

        private static Activity? StartActivity(OutgoingStepContext context)
        {
            var parentActivity = Activity.Current;

            if (parentActivity == null)
            {
                return null;
            }

            Activity? activity = null;
            if (RebusDiagnosticConstants.ActivitySource.HasListeners())
            {

                var message = context.Load<TransportMessage>();
                var messageType = message.GetMessageType();

                var messageWrapper = new TransportMessageWrapper(message);


                var activityKind = messageWrapper.GetIntentOption() == Headers.IntentOptions.PublishSubscribe
                    ? ActivityKind.Producer
                    : ActivityKind.Client;

                var activityName = $"{messageType} send";

                var initialTags = TagHelper.ExtractInitialTags(messageWrapper);

                // Per the spec on how to handle array types: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/common/common.md#attributes
                var destinationAddresses = context.Load<DestinationAddresses>();
                initialTags.Add("messaging.destination", JsonConvert.SerializeObject(destinationAddresses.ToList()));

                // TODO: Transport specific tags, like rabbitmq routing key

                activity = RebusDiagnosticConstants.ActivitySource.StartActivity(activityName, activityKind,
                    parentActivity.Context, initialTags);
                
                TagHelper.CopyBaggage(parentActivity, activity);

                // TODO: Figure out if this is actually needed now
                // DiagnosticListener.OnActivityImport(activity, context);
            }

            SendBeforeSendEvent(context);

            return activity;
        }

        

        private static void SendBeforeSendEvent(OutgoingStepContext context)
        {
            if (DiagnosticListener.IsEnabled(BeforeSendMessage.EventName, context))
            {
                DiagnosticListener.Write(BeforeSendMessage.EventName, new BeforeSendMessage(context));
            }
        }

        private static void SendAfterSendEvent(OutgoingStepContext context)
        {
            if (DiagnosticListener.IsEnabled(AfterSendMessage.EventName))
            {
                DiagnosticListener.Write(AfterSendMessage.EventName, new AfterSendMessage(context));
            }
        }
    }
}