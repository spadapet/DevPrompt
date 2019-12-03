using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Only used for saving custom settings so that plugins are loaded before custom settings are loaded.
    /// Normally these custom settings live in AppSettings, but they are cloned here for persisting.
    /// </summary>
    [DataContract]
    internal sealed class AppCustomSettings
    {
        private Dictionary<string, object> customProperties;

        public AppCustomSettings()
        {
            this.Initialize();
        }

        public AppCustomSettings(AppSettings settings)
        {
            this.Initialize();

            foreach (KeyValuePair<string, object> pair in settings.CustomProperties)
            {
                this.customProperties[pair.Key] = pair.Value;
            }
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.customProperties = new Dictionary<string, object>();
        }

        [DataMember]
        public ICollection<KeyValuePair<string, object>> CustomProperties => this.customProperties;
    }
}
