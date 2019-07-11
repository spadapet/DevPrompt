using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DevPrompt.Utility.Json
{
    internal enum JsonValueType
    {
        Unset,
        Null,
        Bool,
        Number,
        String,
        Array,
        Dictionary,
    }

    [DebuggerDisplay("{DebuggerString}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    internal struct JsonValue : Api.IJsonValue
    {
        public JsonValueType Type { get; }
        private JsonToken token;
        private object value;
        private string json;

        public JsonValue(JsonValueType type, JsonToken token, string json)
        {
            this.Type = type;
            this.token = token;
            this.value = null;
            this.json = json;
        }

        public JsonValue(JsonValueType type, object value, string json)
            : this()
        {
            this.Type = type;
            this.token = default;
            this.value = value;
            this.json = json;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsArray => this.Type == JsonValueType.Array;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsBool => this.Type == JsonValueType.Bool;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsDictionary => this.Type == JsonValueType.Dictionary;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNull => this.Type == JsonValueType.Null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNumber => this.Type == JsonValueType.Number;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsString => this.Type == JsonValueType.String;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsUnset => this.Type == JsonValueType.Unset;

        public static explicit operator bool(JsonValue value) => value.Bool;
        public static explicit operator int(JsonValue value) => value.Int;
        public static explicit operator double(JsonValue value) => value.Double;
        public static explicit operator string(JsonValue value) => value.String;

        public bool Bool
        {
            get
            {
                if (this.IsBool)
                {
                    return this.token.Type == JsonTokenType.True;
                }

                throw new InvalidOperationException($"JsonValue not a Bool: {this.Type}");
            }
        }

        public int Int => (int)this.Double;

        public double Double
        {
            get
            {
                if (this.IsNumber)
                {
                    string str = this.token.GetText(this.json);
                    if (double.TryParse(str, out double value))
                    {
                        return value;
                    }
                }

                throw new InvalidOperationException($"JsonValue not a Number: {this.Type}");
            }
        }

        public string String
        {
            get
            {
                switch (this.Type)
                {
                    case JsonValueType.Null:
                    case JsonValueType.Bool:
                    case JsonValueType.Number:
                        return this.token.GetText(this.json);

                    case JsonValueType.String:
                        return this.token.GetDecodedString(this.json);

                    default:
                        throw new InvalidOperationException($"JsonValue not a String: {this.Type}");
                }
            }
        }

        public IReadOnlyList<JsonValue> Array
        {
            get
            {
                if (this.IsArray && this.value is IReadOnlyList<JsonValue> value)
                {
                    return value;
                }

                throw new InvalidOperationException($"JsonValue not an Array: {this.Type}");
            }
        }

        public IReadOnlyDictionary<string, JsonValue> Dictionary
        {
            get
            {
                if (this.IsDictionary && this.value is IReadOnlyDictionary<string, JsonValue> value)
                {
                    return value;
                }

                throw new InvalidOperationException($"JsonValue not a Dictionary: {this.Type}");
            }
        }

        public JsonValue this[string path] => this.GetFromPath(path, 0);

        public JsonValue this[int index]
        {
            get
            {
                if (this.IsArray && index >= 0 && index < this.Array.Count)
                {
                    return this.Array[index];
                }

                return default;
            }
        }

        private JsonValue GetFromPath(string path, int pos)
        {
            if (path == null || pos >= path.Length)
            {
                return this;
            }

            if (path[pos] == '[')
            {
                int endPos = path.IndexOf(']', pos + 1);
                if (endPos > pos + 1 && int.TryParse(path.Substring(pos + 1, endPos - pos - 1), out int result))
                {
                    return this[result].GetFromPath(path, endPos + 1);
                }
            }
            else if (this.IsDictionary)
            {
                if (pos > 0)
                {
                    if (path[pos] == '.')
                    {
                        pos++;
                    }
                    else
                    {
                        return default;
                    }
                }

                int endPos = path.IndexOfAny(".[".ToCharArray(), pos);
                if (endPos == -1)
                {
                    endPos = path.Length;
                }

                string name = path.Substring(pos, endPos - pos);
                if (this.Dictionary.TryGetValue(name, out JsonValue result))
                {
                    return result.GetFromPath(path, endPos);
                }
            }

            return default;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerString
        {
            get
            {
                switch (this.Type)
                {
                    case JsonValueType.Unset:
                        return "<unset>";

                    case JsonValueType.Null:
                    case JsonValueType.Bool:
                    case JsonValueType.Number:
                    case JsonValueType.String:
                        return this.String;

                    case JsonValueType.Array:
                        return $"Array, Count={this.Array.Count}";

                    case JsonValueType.Dictionary:
                        return $"Dictionary, Count={this.Dictionary.Count}";

                    default:
                        return null;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object DebuggerValue
        {
            get
            {
                switch (this.Type)
                {
                    case JsonValueType.Bool:
                        return this.Bool;

                    case JsonValueType.Number:
                        return this.Double;

                    case JsonValueType.String:
                        return this.String;

                    case JsonValueType.Array:
                        return this.Array;

                    case JsonValueType.Dictionary:
                        return this.Dictionary;

                    default:
                        return null;
                }
            }
        }

        IEnumerator<Api.IJsonValue> IEnumerable<Api.IJsonValue>.GetEnumerator()
        {
            return this.Array.Select(v => (Api.IJsonValue)v).GetEnumerator();
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.ContainsKey(string key)
        {
            return this.Dictionary.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.TryGetValue(string key, out Api.IJsonValue value)
        {
            if (this.Dictionary.TryGetValue(key, out JsonValue jsonValue))
            {
                value = jsonValue;
                return true;
            }

            value = default;
            return false;
        }

        IEnumerator<KeyValuePair<string, Api.IJsonValue>> IEnumerable<KeyValuePair<string, Api.IJsonValue>>.GetEnumerator()
        {
            return this.Dictionary.Select(pair => new KeyValuePair<string, Api.IJsonValue>(pair.Key, pair.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable<Api.IJsonValue> enumerable = this;
            IEnumerator<Api.IJsonValue> enumerator = enumerable.GetEnumerator();
            return enumerator;
        }

        int IReadOnlyCollection<Api.IJsonValue>.Count => this.Array.Count;

        IEnumerable<string> IReadOnlyDictionary<string, Api.IJsonValue>.Keys => this.Dictionary.Keys;

        IEnumerable<Api.IJsonValue> IReadOnlyDictionary<string, Api.IJsonValue>.Values => this.Dictionary.Values.Select(v => (Api.IJsonValue)v);

        int IReadOnlyCollection<KeyValuePair<string, Api.IJsonValue>>.Count => this.Dictionary.Count;

        Api.IJsonValue IReadOnlyDictionary<string, Api.IJsonValue>.this[string key] => this[key];

        Api.IJsonValue IReadOnlyList<Api.IJsonValue>.this[int index] => this[index];

        // Just for the VS debugger
        public class DebugView
        {
            private JsonValue value;

            public DebugView(JsonValue value)
            {
                this.value = value;
            }

            public JsonValueType Type => this.value.Type;
            public object Value => this.value.DebuggerValue;
        }
    }
}
