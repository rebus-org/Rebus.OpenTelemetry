using OpenTelemetry.Trace;
using Rebus.Diagnostics;

namespace Rebus.OpenTelemetry.Configuration
{
    public static class TraceBuilderExtensions
    {
        public static TracerProviderBuilder AddRebusCollectors(this TracerProviderBuilder builder)
        {
            return builder.AddSource(RebusDiagnosticConstants.ActivitySourceName);
        }
    }
}