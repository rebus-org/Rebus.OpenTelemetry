using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
        private static readonly Meter Meter = RebusDiagnosticConstants.Meter;

        private static readonly Histogram<int> _messageDelay = Meter.CreateHistogram<int>(RebusDiagnosticConstants.MessageDelayMeterName, "milliseconds", "milliseconds delay before send");
        private static readonly Histogram<int> _messageSize = Meter.CreateHistogram<int>(RebusDiagnosticConstants.MessageSizeMeterName, "bytes", "Size of message");
        private static readonly Counter<int> _messageSend = Meter.CreateCounter<int>(RebusDiagnosticConstants.MessageSendMeterName, "messages", "number of messages send");

        private readonly Func<DateTime> _nowProvider;

        public OutgoingDiagnosticsStep(Func<DateTime>? nowProvider = null)
        {
            _nowProvider = nowProvider ?? (() => DateTime.UtcNow);
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();

            using var activity = StartActivity(context, message);

            var typeTag = new KeyValuePair<string, object?>("type", message.GetMessageType());
            _messageSize.Record(message.Body.Length, typeTag);
            _messageDelay.Record((int)SentDelay(message, _nowProvider()).TotalMilliseconds, typeTag);
            _messageSend.Add(1, typeTag);

            InjectHeaders(activity, message);

            try
            {
                await next();
            }
            finally
            {
                SendAfterSendEvent(context);
            }
        }

        private static void InjectHeaders(Activity? activity, TransportMessage message)
        {
            if (activity == null) return;
            
            var headers = message.Headers;

            if (!headers.ContainsKey(RebusDiagnosticConstants.TraceStateHeaderName))
            {
                headers[RebusDiagnosticConstants.TraceStateHeaderName] = activity.Id;
            }

            if (!headers.ContainsKey(RebusDiagnosticConstants.BaggageHeaderName))
            {
                headers[RebusDiagnosticConstants.BaggageHeaderName] = JsonConvert.SerializeObject(activity.Baggage);
            }
        }

        private static Activity? StartActivity(OutgoingStepContext context, TransportMessage message)
        {
            var parentActivity = Activity.Current;

            if (parentActivity == null)
            {
                return null;
            }

            Activity? activity = null;
            if (RebusDiagnosticConstants.ActivitySource.HasListeners())
            {
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

        private static TimeSpan SentDelay(TransportMessage message, DateTime date)
        {
            if (!message.Headers.TryGetValue(Headers.SentTime, out var sentTime) 
                || !DateTime.TryParse(sentTime, out var sentDateTime))
            {
                return TimeSpan.Zero;
            }

            return date.Subtract(sentDateTime);
        }
    }
}