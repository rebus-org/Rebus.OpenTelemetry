using System;

using OpenTelemetry.Metrics;

using Rebus.Diagnostics;

namespace Rebus.OpenTelemetry.Configuration
{
    public static class MeterBuilderExtensions
    {
        public static MeterProviderBuilder AddRebusInstrumentation(this MeterProviderBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.AddMeter(RebusDiagnosticConstants.MeterName);
        }
    }
}