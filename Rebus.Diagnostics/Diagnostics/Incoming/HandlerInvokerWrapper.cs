using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Diagnostics.Helpers;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;

namespace Rebus.Diagnostics.Incoming;

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

        var initialTags = new ActivityTagsCollection();
        foreach (var tag in parentActivity.Tags)
        {
            if (!initialTags.ContainsKey(tag.Key))
                initialTags.Add(tag.Key, tag.Value);
        }
        initialTags["messaging.operation"] = "process";
        initialTags["rebus.handler.type"] = _handlerInvokerImplementation.Handler?.GetType().FullName ?? "Unknown handler type";

        using var activity = RebusDiagnosticConstants.ActivitySource.StartActivity($"{_messageType} process", ActivityKind.Internal, parentActivity.Context, initialTags);
            
        TagHelper.CopyBaggage(parentActivity, activity);

        try
        {
            await _handlerInvokerImplementation.Invoke();
        }
        catch (Exception e)
        {
            activity?.AddException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            throw;
        }
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