using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;

namespace Rebus.Diagnostics.Incoming
{
    internal class HandlerInvokerWrapper : HandlerInvoker
    {
        private readonly HandlerInvoker _handlerInvokerImplementation;
        
        private readonly string _messageType;

        internal HandlerInvokerWrapper(HandlerInvoker handlerInvokerImplementation, string messageType)
        {
            _handlerInvokerImplementation = handlerInvokerImplementation;
            _messageType = messageType;
        }

        public override async Task Invoke()
        {
            var parentActivity = Activity.Current;
            if (parentActivity == null)
            {
                await _handlerInvokerImplementation.Invoke();
                return;
            }

            var initialTags = new ActivityTagsCollection
            {
                {"messaging.operation", "process"}
            };
            
            using var activity = Constants.ActivitySource.StartActivity($"{_messageType} process", parentActivity.Kind, parentActivity.Context, initialTags);
            
            await _handlerInvokerImplementation.Invoke();
        }

        public override void SetSagaData(ISagaData sagaData)
        {
            _handlerInvokerImplementation.SetSagaData(sagaData);
        }

        public override ISagaData GetSagaData()
        {
            return _handlerInvokerImplementation.GetSagaData();
        }

        public override void SkipInvocation()
        {
            _handlerInvokerImplementation.SkipInvocation();
        }

        public override bool HasSaga => _handlerInvokerImplementation.HasSaga;

        public override Saga Saga => _handlerInvokerImplementation.Saga;

        public override object Handler => _handlerInvokerImplementation.Handler;

        public override bool WillBeInvoked => _handlerInvokerImplementation.WillBeInvoked;
    }
}