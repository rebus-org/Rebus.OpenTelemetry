using System;
using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;
using Rebus.Config;
using Rebus.OpenTelemetry.Configuration;
using Rebus.Routing.TypeBased;
using WorkerService.Messages;

namespace WorkerService
{

    public class Program
    {
        private const string EndpointName = "RebusbActivities.WorkerService";

        public static void Main(string[] args)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddOpenTelemetry(builder =>
                    {
                        services.AddSingleton(builder);
                        builder
                            .UseZipkin(o =>
                            {
                                o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                                o.ServiceName = EndpointName;
                            })
                            .AddRebusCollectors()
                            .AddRequestCollector()
                            .AddDependencyCollector();
                    });
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterModule<RebusAutofacModule>();
                    builder.RegisterType<SaySomethingHandler>().AsImplementedInterfaces();
                })
        ;
    }
}
