using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Rebus.Diagnostics.Incoming;

[StepDocumentation("Wraps all `HandlerInvoker`s so they can be traced individually")]
public class IncomingDiagnosticsHandlerInvokerWrapper : IIncomingStep
{
    public Task Process(IncomingStepContext context, Func<Task> next)
    {
        var currentHandlerInvokers = context.Load<HandlerInvokers>();

        var message = currentHandlerInvokers.Message;
        var messageType = message.GetMessageType();

        var wrappedHandlerInvokers = currentHandlerInvokers
            .Select(invoker => new HandlerInvokerWrapper(invoker, messageType));

        var updatedHandlerInvokers = new HandlerInvokers(message, wrappedHandlerInvokers);
        context.Save(updatedHandlerInvokers);

        return next();
    }
}