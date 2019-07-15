using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// Global data about a parsed JSON document
    /// </summary>
    internal class JsonContext
    {
        public string Json { get; }
        public JsonValues EmptyArray { get; }
        public JsonValues EmptyDictionary { get; }
        private ConcurrentDictionary<JsonValueData, JsonValue> valueCache;
        private static List<JsonValueData> emptyList = new List<JsonValueData>();
        private static Dictionary<string, JsonValueData> emptyDictionary = new Dictionary<string, JsonValueData>();

        public JsonContext(string json)
        {
            this.Json = json ?? string.Empty;
            this.EmptyArray = new JsonValues(JsonContext.emptyList, this);
            this.EmptyDictionary = new JsonValues(JsonContext.emptyDictionary, this);
            this.valueCache = new ConcurrentDictionary<JsonValueData, JsonValue>();
        }

        public JsonValue GetValue(JsonValueData value)
        {
            return this.valueCache.GetOrAdd(value, v => new JsonValue(v));
        }
    }
}
