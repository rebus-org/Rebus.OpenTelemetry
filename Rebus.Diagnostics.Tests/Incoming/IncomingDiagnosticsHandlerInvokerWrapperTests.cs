using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Rebus.Diagnostics.Incoming;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Rebus.Diagnostics.Tests.Incoming
{
    [TestFixture]
    public class IncomingDiagnosticsHandlerInvokerWrapperTests
    {

        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }
        
        [Test]
        public async Task WrapsInvokersSoTheyCanRunInsideAnActivity()
        {
            var step = new IncomingDiagnosticsHandlerInvokerWrapper();

            var headers = new Dictionary<string, string>
            {
                {Headers.Type, "MyType"}
            };
            var transportMessage = new TransportMessage(headers, Array.Empty<byte>());
            var message = new Message(headers, Array.Empty<byte>());

            var innerInvoker = new TestInvoker(() => { });

            var handlerInvokers = new HandlerInvokers(message, new[] {innerInvoker});

            using var scope = new RebusTransactionScope();
            var context = new IncomingStepContext(transportMessage, scope.TransactionContext);
            context.Save(handlerInvokers);

            var callbackWasInvoked = false;
            await step.Process(context, () =>
            {
                callbackWasInvoked = true;
                return Task.CompletedTask;
            });

            Assert.That(callbackWasInvoked);

            var updatedInvokers = context.Load<HandlerInvokers>();
            Assert.That(updatedInvokers, Is.Not.SameAs(handlerInvokers));
            Assert.That(updatedInvokers, Has.Exactly(1).Items.And.All.TypeOf<HandlerInvokerWrapper>());
        }
    }
}