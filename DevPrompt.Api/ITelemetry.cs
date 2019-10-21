using System.Collections.Generic;

namespace DevPrompt.Api
{
    public interface ITelemetry
    {
        void TrackEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties = null);
    }
}
