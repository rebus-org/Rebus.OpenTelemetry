using Autofac;
using Rebus.Config;
using Rebus.OpenTelemetry.Configuration;
using Rebus.Routing.TypeBased;


namespace WorkerService.Messages
{
    public class SaySomething
    {
        public string Message { get; set; }
    }

    public class SaySomethingResponse
    {
        public string Message { get; set; }
    }

    public class RebusAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            const string inputQueueName = "test";
            builder.RegisterRebus((configurer, context) =>
                configurer
                    .Transport(t => t.UseRabbitMq("amqps://test:test@localhost", inputQueueName))
                    .Routing(r => r.TypeBased().MapAssemblyOf<SaySomething>(inputQueueName))
                    .Logging(l => l.ColoredConsole())
                    .Options(o =>
                    {
                        o.EnableOpenTelemetry();
                    })
            );
        }
    }
}
