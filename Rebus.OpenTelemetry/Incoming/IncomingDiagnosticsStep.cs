using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Incoming
{
    public class IncomingDiagnosticsStep : IIncomingStep
    {
        private static readonly DiagnosticSource DiagnosticListener = new DiagnosticListener(Constants.ConsumerActivityName);

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var activity = StartActivity(context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private static Activity StartActivity(IncomingStepContext context)
        {
            var activity = new Activity(Constants.ConsumerActivityName);

            var headers = context.Load<TransportMessage>().Headers;

            if (!headers.TryGetValue(Constants.TraceParentHeaderName, out var requestId))
            {
                headers.TryGetValue(Constants.RequestIdHeaderName, out requestId);
            }

            if (!string.IsNullOrEmpty(requestId))
            {
                activity.SetParentId(requestId);
                if (headers.TryGetValue(Constants.TraceStateHeaderName, out var traceState))
                {
                    activity.TraceStateString = traceState;
                }

                if (headers.TryGetValue(Constants.CorrelationContextHeaderName, out var correlationContext))
                {
                    var baggage = correlationContext.Split(',');
                    if (baggage.Length > 0)
                    {
                        foreach (var item in baggage)
                        {
                            if (NameValueHeaderValue.TryParse(item, out var baggageItem))
                            {
                                activity.AddBaggage(baggageItem.Name, HttpUtility.UrlDecode(baggageItem.Value));
                            }
                        }
                    }
                }
            }

            DiagnosticListener.OnActivityImport(activity, context);

            activity.Start();

            if (DiagnosticListener.IsEnabled(BeforeProcessMessage.EventName, context))
            {
                DiagnosticListener.Write(BeforeProcessMessage.EventName, new BeforeProcessMessage(context, activity));
            }

            return activity;
        }

        private static void StopActivity(Activity activity, IncomingStepContext context)
        {
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }

            if (DiagnosticListener.IsEnabled(AfterProcessMessage.EventName))
            {
                DiagnosticListener.Write(AfterProcessMessage.EventName, new AfterProcessMessage(context, activity));
            }

            activity.Stop();
        }
    }

}