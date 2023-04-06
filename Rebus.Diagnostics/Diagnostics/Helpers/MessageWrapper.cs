using System.Collections.Generic;
using System.Diagnostics;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;

namespace Rebus.Diagnostics.Helpers;

/// <summary>
/// Wraps Message and TransportMessage so they can be used the same way
/// </summary>
internal abstract class MessageWrapper
{
    protected abstract Dictionary<string, string> Headers { get; }

    internal abstract string GetMessageId();

    internal abstract string GetMessageType();

    public string GetIntentOption()
    {
        return Headers.TryGetValue(Rebus.Messages.Headers.Intent, out var header)
                ? header
                : Rebus.Messages.Headers.IntentOptions.PublishSubscribe;
    }
        
    internal string GetCorrectionId()
    {
        return Headers.GetValueOrNull(Rebus.Messages.Headers.Intent) ?? GetMessageId();
    }
}

internal class TransportMessageWrapper : MessageWrapper
{
    private readonly TransportMessage _message;
        
    internal TransportMessageWrapper(TransportMessage message) => _message = message;

    protected override Dictionary<string, string> Headers => _message.Headers;
    internal override string GetMessageId() => _message.GetMessageId();

    internal override string GetMessageType() => _message.GetMessageType();
}
    
internal class MessageMessageWrapper : MessageWrapper
{
    private readonly Message _message;
        
    internal MessageMessageWrapper(Message message) => _message = message;

    protected override Dictionary<string, string> Headers => _message.Headers;
    internal override string GetMessageId() => _message.GetMessageId();
    internal override string GetMessageType() => _message.GetMessageType();
}