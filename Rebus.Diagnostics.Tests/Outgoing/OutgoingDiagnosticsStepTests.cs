using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Diagnostics.Outgoing;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Send;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Diagnostics.Tests.Outgoing
{
    [TestFixture]
    public class OutgoingDiagnosticsStepTests
    {
        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }
        
        [Test]
        public async Task StartsNoActivityIfThereIsNoCurrentActivity()
        {
            var step = new OutgoingDiagnosticsStep();

            var message = new Message(GetMessageHeaders("id", Headers.IntentOptions.PublishSubscribe), new object());

            var destinations = new DestinationAddresses(new List<string> {"MyQueue"});

            var context = new OutgoingStepContext(message, AmbientTransactionContext.Current, destinations);

            var hadActivity = false;
            
            await step.Process(context, () =>
            {
                hadActivity = Activity.Current != null;
                return Task.CompletedTask;
            });
            
            Assert.That(hadActivity, Is.False);
        }

        [Test]
        public async Task StartsNewActivityIfThereIsAlreadyAParentActivity()
        {
            Assume.That(Constants.ActivitySource.HasListeners(), Is.True);
            
            var step = new OutgoingDiagnosticsStep();

            var message = new Message(GetMessageHeaders("id", Headers.IntentOptions.PublishSubscribe), new object());

            var destinations = new DestinationAddresses(new List<string> {"MyQueue"});

            var context = new OutgoingStepContext(message, AmbientTransactionContext.Current, destinations);

            using var activity = new Activity("MyActivity");
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
        
        
        // private IBus CreateBus(InMemNetwork network)
        // {
        //     
        // }
    }
}