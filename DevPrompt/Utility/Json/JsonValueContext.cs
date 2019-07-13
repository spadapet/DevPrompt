using System.Collections.Concurrent;

namespace DevPrompt.Utility.Json
{
    internal class JsonValueContext
    {
        public string Json { get; }
        private ConcurrentDictionary<JsonValue, JsonDynamic> dynamicCache;
        private ConcurrentDictionary<JsonValue, Api.IJsonValue> interfaceCache;

        public JsonValueContext(string json)
        {
            this.Json = json ?? string.Empty;
            this.dynamicCache = new ConcurrentDictionary<JsonValue, JsonDynamic>();
            this.interfaceCache = new ConcurrentDictionary<JsonValue, Api.IJsonValue>();
        }

        public dynamic GetDynamic(JsonValue value)
        {
            return this.dynamicCache.GetOrAdd(value, JsonValueContext.CreateJsonDynamic);
        }

        public Api.IJsonValue GetInterface(JsonValue value)
        {
            return this.interfaceCache.GetOrAdd(value, JsonValueContext.CreateInterface);
        }

        private static JsonDynamic CreateJsonDynamic(JsonValue value)
        {
            return new JsonDynamic(value);
        }

        private static Api.IJsonValue CreateInterface(JsonValue value)
        {
            // move to the heap
            return value;
        }
    }
}
