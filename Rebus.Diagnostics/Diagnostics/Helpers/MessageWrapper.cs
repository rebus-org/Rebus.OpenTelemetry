using System.Collections.Generic;
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

    internal string GetMessageType()
    {
        return Headers.GetValueOrNull(Rebus.Messages.Headers.Type) ?? "";
    }
}

internal class TransportMessageWrapper : MessageWrapper
{
    private readonly TransportMessage _message;
        
    internal TransportMessageWrapper(TransportMessage message) => _message = message;

    protected override Dictionary<string, string> Headers => _message.Headers;
    internal override string GetMessageId() => _message.GetMessageId();
}