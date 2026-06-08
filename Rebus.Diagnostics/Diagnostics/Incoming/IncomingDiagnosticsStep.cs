using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Diagnostics.Helpers;
using Rebus.Diagnostics.Outgoing;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.Diagnostics.Incoming
{
    [StepDocumentation("Extracts trace from the incoming message and starts an activity for it")]
    public class IncomingDiagnosticsStep : IIncomingStep
    {
        private readonly ILog _log;

        private static readonly DiagnosticSource DiagnosticListener =
            new DiagnosticListener(RebusDiagnosticConstants.ConsumerActivityName);
        private readonly StepMeter _stepMeter;

        public IncomingDiagnosticsStep(ILog log)
        {
            _log = log;
            _stepMeter = new StepMeter("incoming");
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();

            using var activity = StartActivity(context, message);

            _stepMeter.Observe(message);
            
            try
            {
                await next();
            }
            finally
            {
                SendAfterProcessEvent(activity, context);
            }
        }

        private Activity? StartActivity(IncomingStepContext context, TransportMessage message)
        {
            try
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
                        activity = RebusDiagnosticConstants.ActivitySource.StartActivity(activityName, activityKind,
                            default(ActivityContext), initialTags);
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
            catch (Exception e)
            {
                _log.Warn(e, "Failed to start message activity. Continuing without");
                return null;
            }
        }

        private void CopyBaggage(Dictionary<string, string> headers, Activity activity)
        {
            if (headers.TryGetValue(RebusDiagnosticConstants.BaggageHeaderName, out var baggageContent))
            {
                try
                {
                    var baggage =
                        JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(baggageContent);

                    if (baggage == null)
                    {
                        return;
                    }
                    
                    foreach (var keyValuePair in baggage)
                    {
                        activity.AddBaggage(keyValuePair.Key, keyValuePair.Value);
                    }
                }
                catch (Exception e)
                {
                    _log.Warn(e, "Failed to process activity baggage: {0}", baggageContent);
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