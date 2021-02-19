using System;
using System.Threading.Tasks;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;

namespace Rebus.Diagnostics.Tests.Incoming
{
    public class TestInvoker : HandlerInvoker
    {
        private readonly Action _invoke;

        public TestInvoker(Action invoke)
        {
            _invoke = invoke;
        }

        public override Task Invoke()
        {
            _invoke();
            return Task.CompletedTask;
        }

        public override void SetSagaData(ISagaData sagaData)
        {
            throw new NotImplementedException();
        }

        public override ISagaData GetSagaData()
        {
            throw new NotImplementedException();
        }

        public override void SkipInvocation()
        {
            throw new NotImplementedException();
        }

        public override bool HasSaga => false;
        public override Saga Saga => null!;
        public override object Handler => null!;
        public override bool WillBeInvoked => true;
    }
}