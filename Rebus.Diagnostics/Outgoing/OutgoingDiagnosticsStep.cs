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
        private static readonly DiagnosticSource DiagnosticListener = new DiagnosticListener(Constants.ProducerActivityName);

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            using var activity = StartActivity(context);
            
            InjectHeaders(activity, context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                SendAfterSendEvent(context);
            }
        }

        private static void InjectHeaders(Activity? activity, OutgoingStepContext context)
        {
            if (activity == null) return;
            
            var headers = context.Load<Message>().Headers;

            if (!headers.ContainsKey(Constants.TraceStateHeaderName))
            {
                headers[Constants.TraceStateHeaderName] = activity.TraceStateString;
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
            if (Constants.ActivitySource.HasListeners())
            {

                var message = context.Load<Message>();
                var messageType = message.GetMessageType();

                var messageWrapper = new MessageMessageWrapper(message);


                var activityKind = messageWrapper.GetIntentOption() == Headers.IntentOptions.PublishSubscribe
                    ? ActivityKind.Producer
                    : ActivityKind.Client;

                var activityName = $"{messageType} send";

                var initialTags = TagHelper.ExtractInitialTags(messageWrapper);

                // Per the spec on how to handle array types: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/common/common.md#attributes
                var destinationAddresses = context.Load<DestinationAddresses>();
                initialTags.Add("messaging.destination", JsonConvert.SerializeObject(destinationAddresses.ToList()));

                // TODO: Transport specific tags, like rabbitmq routing key

                activity = Constants.ActivitySource.StartActivity(activityName, activityKind,
                    parentActivity.Context, initialTags);

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