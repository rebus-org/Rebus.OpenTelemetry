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
    internal class StepMeter
    {
        private readonly Histogram<int> _messageSize;
        private readonly Histogram<int> _messageDelay;
        private readonly Counter<int> _messagesCount;

        public StepMeter(string direction)
        {
            var meter = RebusDiagnosticConstants.Meter;
            _messageSize = meter.CreateHistogram<int>(string.Format(RebusDiagnosticConstants.MessageSizeMeterNameTemplate, direction), "bytes", "message body size");
            _messageDelay = meter.CreateHistogram<int>(string.Format(RebusDiagnosticConstants.MessageDelayMeterNameTemplate, direction), "milliseconds", "milliseconds since creation");
            _messagesCount = meter.CreateCounter<int>(string.Format(RebusDiagnosticConstants.MessageCountMeterNameTemplate, direction), "messages", $"number of messages {direction}");
        }

        public void Observe(TransportMessage message)
        {
            var messageType = message.GetMessageType();

            var tags = new KeyValuePair<string, object?>("type", messageType);

            _messageSize.Record(message.Body.Length, tags);
            _messagesCount.Add(1, tags);

            var delay = SentDelay(message, DateTime.UtcNow);
            _messageDelay.Record((int)delay.TotalMilliseconds, tags);
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

    [StepDocumentation("Creates a new activity for sending the provided message and passes it along on the message")]
    public class OutgoingDiagnosticsStep : IOutgoingStep
    {
        private static readonly DiagnosticSource DiagnosticListener = new DiagnosticListener(RebusDiagnosticConstants.ProducerActivityName);
        private readonly StepMeter _stepMeter;

        public OutgoingDiagnosticsStep()
        {
            _stepMeter = new StepMeter("outgoing");
        }

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();

            using var activity = StartActivity(context, message);

            _stepMeter.Observe(message);
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
    }
}