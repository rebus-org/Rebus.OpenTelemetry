using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rebus.Diagnostics.Tests
{
    public static class TestHelpers
    {
        public static void ListenForRebus()
        {
            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = source => source.Name == RebusDiagnosticConstants.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });
        }

        private class AnonymousObserver<T> : IObserver<T>
        {
            private Action<T> _handle;

            public AnonymousObserver(Action<T> handle)
            {
                _handle = handle;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(T value)
            {
                _handle(value);
            }
        }
    }
}