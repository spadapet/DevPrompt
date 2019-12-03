using System;
using System.Collections.Generic;

namespace DevPrompt.Api
{
    /// <summary>
    /// For tracking feature usage or other interesting events
    /// </summary>
    public interface ITelemetry
    {
        void TrackEvent(string eventName, IEnumerable<KeyValuePair<string, object>> properties = null);
        void TrackException(Exception exception, IEnumerable<KeyValuePair<string, object>> properties = null);
    }
}
