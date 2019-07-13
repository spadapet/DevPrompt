using System.Collections.Concurrent;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// Global data about a JSON document that all values reference
    /// </summary>
    internal class JsonValueContext
    {
        public string Json { get; }
        private ConcurrentDictionary<JsonValue, Api.IJsonValue> interfaceCache;

        public JsonValueContext(string json)
        {
            this.Json = json ?? string.Empty;
            this.interfaceCache = new ConcurrentDictionary<JsonValue, Api.IJsonValue>();
        }

        /// <summary>
        /// Cache values that get moved to the heap
        /// </summary>
        public Api.IJsonValue GetInterface(JsonValue value)
        {
            return this.interfaceCache.GetOrAdd(value, v => v);
        }
    }
}
