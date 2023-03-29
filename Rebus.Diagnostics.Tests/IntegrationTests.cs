using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;

namespace Rebus.Diagnostics.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }
        
        [Test]
        public async Task ActuallyPassesActivityToHandlerOnOtherSide()
        {
            var network = new InMemNetwork();
            var subscriberStore = new InMemorySubscriberStore();

            using var publisherActivator = new BuiltinHandlerActivator();
            using var subscriberActivator = new BuiltinHandlerActivator();
            using var eventWasReceived = new ManualResetEvent(initialState: false);

            var publisher = Configure.With(publisherActivator)
                .Transport(t => t.UseInMemoryTransport(network, "publisher"))
                .Options(o => o.EnableDiagnosticSources())
                .Start();

            var rootActivity = new Activity("root");
            rootActivity.SetIdFormat(ActivityIdFormat.W3C);

            subscriberActivator.Handle<string>(_ =>
            {
                var act = Activity.Current!;
                Assert.That(act, Is.Not.Null);
                Assert.That(act.RootId, Is.EqualTo(rootActivity.RootId));
                Assert.That(act.Id, Is.Not.EqualTo(rootActivity.RootId));
                
                // ReSharper disable once AccessToDisposedClosure
                eventWasReceived.Set();
                
                Assert.That(act.GetBaggageItem("MyBaggage"), Is.EqualTo("Hej Verden!"));

                return Task.CompletedTask;
            });

            var subscriber = Configure.With(subscriberActivator)
                .Transport(t => t.UseInMemoryTransport(network, "subscriber"))
                .Options(o => o.EnableDiagnosticSources())
                .Start();

            await subscriber.Subscribe<string>();

            rootActivity.AddBaggage("MyBaggage", "Hej Verden!");
            rootActivity.Start();
            await publisher.Publish("Super Duper fed besked");
            
            Assert.That(eventWasReceived.WaitOne(TimeSpan.FromSeconds(5)), Is.True, "Did not receive the published event within 5 seconds");


        }
    }
}