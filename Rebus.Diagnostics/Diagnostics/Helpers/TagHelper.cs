using System.Diagnostics;
using Rebus.Messages;

namespace Rebus.Diagnostics.Helpers;

internal static class TagHelper
{
    internal static ActivityTagsCollection ExtractInitialTags(MessageWrapper message)
    {
        var initialTags = new ActivityTagsCollection();
            
        var kind = message.GetIntentOption() switch
        {
            Headers.IntentOptions.PublishSubscribe => "topic",
            Headers.IntentOptions.PointToPoint => "queue",
            _ => "<unknown>"
        };
        initialTags.Add("messaging.destination_kind", kind);
        initialTags.Add("messaging.message_id", message.GetMessageId());
        initialTags.Add("messaging.conversation_id", message.GetCorrectionId());

        return initialTags;
    }
        
    internal static void CopyBaggage(Activity parentActivity, Activity? activity)
    {
        if (activity == null) return;
            
        foreach (var keyValuePair in parentActivity.Baggage)
        {
            activity?.AddBaggage(keyValuePair.Key, keyValuePair.Value);
        }
    }
}