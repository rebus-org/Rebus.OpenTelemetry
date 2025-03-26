using System;
using System.Diagnostics;
using System.Linq;
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
        public void MarksActivityAsFailedIfHandlerThrows()
        {
            using var activity = new Activity("MyActivity");

            Activity? innerActivity = null;
            var innerInvoker = new TestInvoker(() =>
            {
                innerActivity = Activity.Current;
                throw new Exception("Look im failing");
            });

            var wrapper = new HandlerInvokerWrapper(innerInvoker, "MyMessage");

            activity.Start();

            Assume.That(activity, Is.SameAs(Activity.Current));

            Assert.That(async () =>
            {
                await wrapper.Invoke();
            }, Throws.Exception);

            
            Assert.That(innerActivity, Is.Not.Null);

            Assert.That(innerActivity!.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(innerActivity!.StatusDescription, Is.EqualTo("Look im failing"));
            Assert.That(innerActivity.Events, Has.Exactly(1).Items);
            var ev = innerActivity.Events.Single();
            Assert.That(ev.Tags.FirstOrDefault(t => t.Key == "exception.type").Value, Is.EqualTo("System.Exception"));
            Assert.That(ev.Tags.FirstOrDefault(t => t.Key == "exception.message").Value, Is.EqualTo("Look im failing"));
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