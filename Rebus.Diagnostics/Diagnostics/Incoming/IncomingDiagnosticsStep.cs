using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
            new DiagnosticListener(RebusDiagnosticConstants.ConsumerActivityName);
        private static readonly Meter Meter = RebusDiagnosticConstants.Meter;

        private static readonly Counter<int> _messageReceived = Meter.CreateCounter<int>("message.received", "messages", "number of messages received");

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();

            using var activity = StartActivity(context, message);

            var typeTag = new KeyValuePair<string, object?>("type", message.GetMessageType());
            _messageReceived.Add(1, typeTag);

            try
            {
                await next();
            }
            finally
            {
                SendAfterProcessEvent(activity, context);
            }
        }

        private static Activity? StartActivity(IncomingStepContext context, TransportMessage message)
        {
            Activity? activity = null;
            if (RebusDiagnosticConstants.ActivitySource.HasListeners())
            {
                var headers = message.Headers;

                var messageType = message.GetMessageType();

                var messageWrapper = new TransportMessageWrapper(message);
            
                var initialTags = TagHelper.ExtractInitialTags(messageWrapper);
                initialTags.Add("messaging.operation", "receive");

                var activityKind = messageWrapper.GetIntentOption() == Headers.IntentOptions.PublishSubscribe
                    ? ActivityKind.Consumer
                    : ActivityKind.Server;

                var activityName = $"{messageType} receive";
                if (!headers.TryGetValue(RebusDiagnosticConstants.TraceStateHeaderName, out var traceState))
                {
                    activity = RebusDiagnosticConstants.ActivitySource.StartActivity(activityName, activityKind, default(ActivityContext), initialTags);
                }
                else
                {
                    activity = RebusDiagnosticConstants.ActivitySource.StartActivity(activityName, activityKind,
                        traceState, initialTags);
                }

                if (activity != null)
                {
                    CopyBaggage(headers, activity);
                }

                // TODO: Not sure if this is still needed
                // DiagnosticListener.OnActivityImport(activity, context);
            }
            
            SendBeforeProcessEvent(context, activity);

            return activity;
        }

        private static void CopyBaggage(Dictionary<string, string> headers, Activity activity)
        {
            if (headers.TryGetValue(RebusDiagnosticConstants.BaggageHeaderName, out var baggageContent))
            {
                var baggage =
                    JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(baggageContent);

                foreach (var keyValuePair in baggage)
                {
                    activity.AddBaggage(keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        private static void SendBeforeProcessEvent(IncomingStepContext context, Activity? activity)
        {
            if (DiagnosticListener.IsEnabled(BeforeProcessMessage.EventName, context))
            {
                DiagnosticListener.Write(BeforeProcessMessage.EventName, new BeforeProcessMessage(context, activity));
            }
        }

        private static void SendAfterProcessEvent(Activity? activity, IncomingStepContext context)
        {
            if (DiagnosticListener.IsEnabled(AfterProcessMessage.EventName))
            {
                DiagnosticListener.Write(AfterProcessMessage.EventName, new AfterProcessMessage(context, activity));
            }
        }
    }
}