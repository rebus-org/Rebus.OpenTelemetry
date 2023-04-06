using System;

using OpenTelemetry.Trace;
using Rebus.Diagnostics;

namespace Rebus.OpenTelemetry.Configuration;

public static class TraceBuilderExtensions
{
    public static TracerProviderBuilder AddRebusInstrumentation(this TracerProviderBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        return builder.AddSource(RebusDiagnosticConstants.ActivitySourceName);
    }
}