using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Diagnostics.Incoming;

namespace Rebus.Diagnostics.Tests.Incoming
{
    [TestFixture]
    public class HandlerInvokerWrapperTests
    {
        [OneTimeSetUp]
        public static void ListenForRebus()
        {
            TestHelpers.ListenForRebus();
        }

        [Test]
        public async Task CreatesNewSubActivityIfThereIsAnActiveActivity()
        {
            using var activity = new Activity("MyActivity");
            var innerInvokerWasInvoked = false;
            var hadActivity = false;

            var innerInvoker = new TestInvoker(() =>
            {
                innerInvokerWasInvoked = true;
                hadActivity = Activity.Current != null;
                // ReSharper disable once AccessToDisposedClosure
                Assert.That(Activity.Current!.ParentId, Is.EqualTo(activity.Id));
            });

            var wrapper = new HandlerInvokerWrapper(innerInvoker, "MyMessage");

            activity.Start();

            Assume.That(activity, Is.SameAs(Activity.Current));

            await wrapper.Invoke();

            Assert.That(innerInvokerWasInvoked);
            Assert.That(hadActivity);
        }

        [Test]
        public async Task CreatesNoNewActivityIfThereIsntAlreadyAnActiveActivity()
        {
            var innerInvokerWasInvoked = false;
            var hadActivity = false;

            var innerInvoker = new TestInvoker(() =>
            {
                innerInvokerWasInvoked = true;
                hadActivity = Activity.Current != null;
            });

            var wrapper = new HandlerInvokerWrapper(innerInvoker, "MyMessage");

            await wrapper.Invoke();

            Assert.That(innerInvokerWasInvoked);
            Assert.That(hadActivity, Is.False);
        }
    }
}