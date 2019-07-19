using System.Diagnostics;

namespace DevPrompt.Utility.Json
{
    internal enum JsonValueType
    {
        None,
        Null,
        Bool,
        Number,
        String,
        Array,
        Dictionary,
    }

    /// <summary>
    /// Parsed data for a value
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    internal struct JsonValueData
    {
        public JsonValueType Type { get; }
        public JsonToken Token { get; }
        public JsonValues List { get; }
        public JsonContext Context => this.List.Context;

        public JsonValueData(JsonValueType type, JsonToken token, JsonValues list)
        {
            Debug.Assert(list != null);
            Debug.Assert(token.Start >= 0 && token.Start <= list.Context.Json.Length && token.Start + token.Length <= list.Context.Json.Length);

            this.Type = type;
            this.Token = token;
            this.List = list;
        }

        public JsonValue Value => this.Context.GetValue(this);
        public static JsonValueData None(JsonContext context) => new JsonValueData(JsonValueType.None, JsonToken.None, context.EmptyArray);

        public override int GetHashCode() => HashUtility.CombineHashCodes(
            this.Type.GetHashCode(),
            this.Token.Length.GetHashCode(),
            this.List.GetHashCode(),
            HashUtility.HashSubstring(this.Context.Json, this.Token.Start, this.Token.Length));

        public bool Equals(JsonValueData other)
        {
            if (this.Type != other.Type ||
                this.Token.Length != other.Token.Length ||
                this.List != other.List ||
                this.Context != other.Context)
            {
                return false;
            }

            return string.CompareOrdinal(
                this.Context.Json,
                this.Token.Start,
                other.Context.Json,
                other.Token.Start,
                this.Token.Length) == 0;
        }

        public override bool Equals(object obj) => obj is JsonValueData other && this.Equals(other);
        public static bool operator ==(JsonValueData x, JsonValueData y) => x.Equals(y);
        public static bool operator !=(JsonValueData x, JsonValueData y) => !x.Equals(y);

        public override string ToString()
        {
            switch (this.Type)
            {
                case JsonValueType.None:
                    return "Invalid";

                case JsonValueType.String:
                    return JsonTokenizer.DecodeString(this.Context.Json, this.Token);

                case JsonValueType.Array:
                    return $"Array, Count={this.List.Array.Count}";

                case JsonValueType.Dictionary:
                    return $"Dictionary, Count={this.List.Dictionary.Count}";

                default:
                    return this.Context.Json.Substring(this.Token.Start, this.Token.Length);
            }
        }

        private class DebuggerView
        {
            private JsonValueData value;

            public DebuggerView(JsonValueData value)
            {
                this.value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value => this.value.Value;
        }
    }
}
