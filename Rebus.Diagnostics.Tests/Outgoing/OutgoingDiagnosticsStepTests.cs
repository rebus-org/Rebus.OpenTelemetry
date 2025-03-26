using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Diagnostics.Outgoing;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Tests.Contracts;
using Rebus.Transport;

namespace Rebus.Diagnostics.Tests.Outgoing
{
    [TestFixture]
    public class OutgoingDiagnosticsStepTests : FixtureBase
    {
        RebusTransactionScope _scope = null!;

        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }

        protected override void SetUp()
        {
            _scope = Using(new RebusTransactionScope());
        }

        [Test]
        public async Task StartsNoActivityIfThereIsNoCurrentActivity()
        {
            var step = new OutgoingDiagnosticsStep();

            var headers = GetMessageHeaders("id", Headers.IntentOptions.PublishSubscribe);
            var message = new Message(headers, new object());
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var destinations = new DestinationAddresses(new List<string> {"MyQueue"});

            var context = new OutgoingStepContext(message, _scope.TransactionContext, destinations);
            context.Save(transportMessage);
            
            var hadActivity = false;
            var callbackWasInvoked = false;
            
            await step.Process(context, () =>
            {
                hadActivity = Activity.Current != null;
                callbackWasInvoked = true;
                return Task.CompletedTask;
            });
            
            Assert.That(hadActivity, Is.False);
            Assert.That(headers, Has.No.ContainKey(RebusDiagnosticConstants.TraceStateHeaderName));
            Assert.That(callbackWasInvoked, Is.True);
        }

        [Test]
        public async Task StartsNewActivityIfThereIsAlreadyAParentActivity()
        {
            Assume.That(RebusDiagnosticConstants.ActivitySource.HasListeners(), Is.True);
            
            var step = new OutgoingDiagnosticsStep();

            var headers = GetMessageHeaders("id", Headers.IntentOptions.PublishSubscribe);

            var message = new Message(headers, new object());
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var destinations = new DestinationAddresses(new List<string> {"MyQueue"});

            var context = new OutgoingStepContext(message, _scope.TransactionContext, destinations);
            context.Save(transportMessage);

            using var activity = new Activity("MyActivity");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            
            Assume.That(activity, Is.SameAs(Activity.Current));
            var hadActivity = false;
            var hadExpectedParent = false;
            
            await step.Process(context, () =>
            {
                
                hadActivity = Activity.Current != null;

                hadExpectedParent = Activity.Current?.ParentSpanId == activity.SpanId; 
                return Task.CompletedTask;
            });
            
            Assert.That(hadActivity, Is.True);
            Assert.That(hadExpectedParent, Is.True);
            Assert.That(transportMessage.Headers, Contains.Key(RebusDiagnosticConstants.TraceStateHeaderName));
            Assert.That(transportMessage.Headers[RebusDiagnosticConstants.TraceStateHeaderName], Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task RecordsMessageStatistics()
        {
            var meterObserver = new MeterObserver();
            
            var step = new OutgoingDiagnosticsStep();

            var headers = GetMessageHeaders("id", Headers.IntentOptions.PublishSubscribe);
            var message = new Message(headers, new object());
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());

            var destinations = new DestinationAddresses(new List<string> { "MyQueue" });

            var context = new OutgoingStepContext(message, _scope.TransactionContext, destinations);
            context.Save(transportMessage);

            await step.Process(context, () => Task.CompletedTask);

            Assert.That(meterObserver.InstrumentCalled("outgoing", RebusDiagnosticConstants.MessageCountMeterNameTemplate), Is.True);
            Assert.That(meterObserver.InstrumentCalled("outgoing", RebusDiagnosticConstants.MessageDelayMeterNameTemplate), Is.True);
            Assert.That(meterObserver.InstrumentCalled("outgoing", RebusDiagnosticConstants.MessageSizeMeterNameTemplate), Is.True);
        }

        private Dictionary<string, string> GetMessageHeaders(string messageId, string intent)
        {
            return new Dictionary<string, string>
            {
                {Headers.Type, typeof(OutgoingDiagnosticsStepTests).AssemblyQualifiedName!},
                {Headers.MessageId, messageId},
                {Headers.Intent, intent}
            };
        }
    }
}