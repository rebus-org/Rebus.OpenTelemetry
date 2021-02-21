# Rebus.OpenTelemetry

Makes Rebus emit Diagnostic traces, which OpenTelemetry can be used to generate tracing. 

# Usage

Add the packages:
```c#
Rebus.Diagnostics
Rebus.OpenTelemetry
```

When configuring Rebus call `EnableDiagnosticSources` like this:
```c#
using var publisherActivator = new BuiltinHandlerActivator();

var bus = Configure.With(publisherActivator)
    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Messages"))
    .Options(o => o.EnableDiagnosticSources()) // This is the important line
    .Start();
```

Then add Rebus tracing to your OpenTelemetry calls, by doing `AddRebusInstrumentation`:

```c#
var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddRebusInstrumentation()
                .Build()
```

And then everything should just work. 🙂