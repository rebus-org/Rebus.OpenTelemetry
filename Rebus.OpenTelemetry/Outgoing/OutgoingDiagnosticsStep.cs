using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Rebus.OpenTelemetry.Outgoing
{
    public class OutgoingDiagnosticsStep : IOutgoingStep
    {
        private static readonly DiagnosticSource DiagnosticListener = new DiagnosticListener(Constants.ProducerActivityName);

        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var activity = StartActivity(context);

            InjectHeaders(activity, context);

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                StopActivity(activity, context);
            }
        }

        private static void InjectHeaders(Activity activity, OutgoingStepContext context)
        {
            var headers = context.Load<Message>().Headers;

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                if (!headers.ContainsKey(Constants.TraceParentHeaderName))
                {
                    headers[Constants.TraceParentHeaderName] = activity.Id;
                    if (activity.TraceStateString != null)
                    {
                        headers[Constants.TraceStateHeaderName] = activity.TraceStateString;
                    }
                }
            }
            else
            {
                if (!headers.ContainsKey(Constants.RequestIdHeaderName))
                {
                    headers[Constants.RequestIdHeaderName] = activity.Id;
                }
            }
        }

        private static Activity StartActivity(OutgoingStepContext context)
        {
            var activity = new Activity(Constants.ProducerActivityName);

            DiagnosticListener.OnActivityImport(activity, context);

            activity.Start();

            if (DiagnosticListener.IsEnabled(BeforeSendMessage.EventName, context))
            {
                DiagnosticListener.Write(BeforeSendMessage.EventName, new BeforeSendMessage(context));
            }

            return activity;
        }

        private static void StopActivity(Activity activity, OutgoingStepContext context)
        {
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }

            if (DiagnosticListener.IsEnabled(AfterSendMessage.EventName))
            {
                DiagnosticListener.Write(AfterSendMessage.EventName, new AfterSendMessage(context));
            }

            activity.Stop();
        }
    }
}