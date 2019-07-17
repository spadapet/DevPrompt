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
        public JsonConvert Converter { get; }
        public JsonValues EmptyArray { get; }
        public JsonValues EmptyDictionary { get; }
        private ConcurrentDictionary<JsonValueData, JsonValue> valueCache;
        private ConcurrentDictionary<JsonValue, JsonDynamic> dynamicCache;
        private static List<JsonValueData> emptyList = new List<JsonValueData>();
        private static Dictionary<string, JsonValueData> emptyDictionary = new Dictionary<string, JsonValueData>();

        public JsonContext(string json)
        {
            this.Json = json ?? string.Empty;
            this.Converter = new JsonConvert();
            this.EmptyArray = new JsonValues(JsonContext.emptyList, this);
            this.EmptyDictionary = new JsonValues(JsonContext.emptyDictionary, this);
            this.valueCache = new ConcurrentDictionary<JsonValueData, JsonValue>();
            this.dynamicCache = new ConcurrentDictionary<JsonValue, JsonDynamic>();
        }

        public JsonValue GetValue(JsonValueData value)
        {
            return this.valueCache.GetOrAdd(value, v => new JsonValue(v));
        }

        public JsonDynamic GetDynamic(JsonValue value)
        {
            return this.dynamicCache.GetOrAdd(value, v => new JsonDynamic(v));
        }
    }
}
