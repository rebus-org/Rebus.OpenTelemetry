using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Diagnostics.Incoming;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Diagnostics.Tests.Incoming
{
    [TestFixture]
    public class IncomingDiagnosticsStepTests
    {
        
        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }

        
        [Test]
        public async Task StartsActivityWhenTraceStateHeaderIsSet()
        {
            var activity = new Activity("MyOperation");
            activity.SetIdFormat(ActivityIdFormat.W3C); // Only the default on .net 5. Below that Hierarchical is the default
            activity.Start();

            Assume.That(activity.Id, Is.Not.Null);
            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"},
                {Headers.Intent, Headers.IntentOptions.PublishSubscribe},
                {Headers.MessageId, "MyMessage"},
                {Constants.TraceStateHeaderName, activity.Id!}
            };

            activity.Stop();

            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            var callbackInvoked = false;
            await step.Process(context, () =>
            {
                callbackInvoked = true;
                Assert.That(Activity.Current!.ParentId, Is.EqualTo(activity.Id));
                return Task.CompletedTask;
            });
            
            Assert.That(callbackInvoked);
        }

        [Test]
        public async Task StartsAnEntireNewActivityIfNoActivityIsCurrentlyActive()
        {
            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"},
                {Headers.Intent, Headers.IntentOptions.PublishSubscribe},
                {Headers.MessageId, "MyMessage"},
            };
            
            
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);

            var step = new IncomingDiagnosticsStep();
            var callbackInvoked = false;
            await step.Process(context, () =>
            {
                callbackInvoked = true;
                Assert.That(Activity.Current, Is.Not.Null);
                Assert.That(Activity.Current!.RootId, Is.EqualTo(Activity.Current.TraceId.ToString()));
                return Task.CompletedTask;
            });
            
            Assert.That(callbackInvoked);
        }
    }
}