using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

    internal struct JsonValue : Api.IJsonValue, IEquatable<JsonValue>
    {
        public JsonValueType Type { get; }
        private JsonValueContext context;
        private JsonToken token;
        private object value;

        public JsonValue(JsonValueType type, JsonToken token, JsonValueContext context)
        {
            this.Type = type;
            this.context = context;
            this.token = token;
            this.value = null;
        }

        public JsonValue(JsonValueType type, object value, JsonValueContext context)
            : this()
        {
            this.Type = type;
            this.context = context;
            this.token = default;
            this.value = value;
        }

        public override int GetHashCode()
        {
            return ((int)this.Type << 8) ^
                this.context.GetHashCode() ^
                this.token.GetHashCode() ^
                (this.value != null ? this.value.GetHashCode() : 0);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonValue other && this.Equals(other);
        }

        public bool Equals(JsonValue other)
        {
            return this.Type == other.Type &&
                this.context == other.context &&
                this.token.Equals(other.token) &&
                this.value == other.value;
        }

        public static bool operator ==(JsonValue x, JsonValue y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(JsonValue x, JsonValue y)
        {
            return !x.Equals(y);
        }

        public static explicit operator bool(JsonValue value) => value.Bool;
        public static explicit operator int(JsonValue value) => value.Int;
        public static explicit operator double(JsonValue value) => value.Double;
        public static explicit operator string(JsonValue value) => value.String;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsArray => this.Type == JsonValueType.Array;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsBool => this.Type == JsonValueType.Bool;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsDictionary => this.Type == JsonValueType.Dictionary;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsNull => this.Type == JsonValueType.Null;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsString => this.Type == JsonValueType.String;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsUnset => this.Type == JsonValueType.Unset;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsInt => this.IsNumber && int.TryParse(this.Text, out _);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsDouble => this.IsNumber && double.TryParse(this.Text, out _);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private bool IsNumber => this.Type == JsonValueType.Number;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool Bool
        {
            get
            {
                if (this.IsBool)
                {
                    return this.token.Type == JsonTokenType.True;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Bool));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Int
        {
            get
            {
                if (this.IsNumber && int.TryParse(this.Text, out int value))
                {
                    return value;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Number));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public double Double
        {
            get
            {
                if (this.IsNumber && double.TryParse(this.Text, out double value))
                {
                    return value;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Number));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string String
        {
            get
            {
                switch (this.Type)
                {
                    case JsonValueType.Null:
                    case JsonValueType.Bool:
                    case JsonValueType.Number:
                        return this.Text;

                    case JsonValueType.String:
                        return this.token.GetDecodedString(this.context.Json);

                    default:
                        throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.String));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IReadOnlyList<JsonValue> Array
        {
            get
            {
                if (this.IsArray && this.value is IReadOnlyList<JsonValue> value)
                {
                    return value;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Array));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IReadOnlyDictionary<string, JsonValue> Dictionary
        {
            get
            {
                if (this.IsDictionary && this.value is IReadOnlyDictionary<string, JsonValue> value)
                {
                    return value;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Dictionary));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Api.IJsonValue AsInterface => this.context.GetInterface(this);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string Text => this.token.GetText(this.context.Json);

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

        /// <summary>
        /// Just for debugger display
        /// </summary>
        public override string ToString()
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

        public object Value
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
                    case JsonValueType.Dictionary:
                        return this.value;

                    default:
                        return null;
                }
            }
        }

        IEnumerator<Api.IJsonValue> IEnumerable<Api.IJsonValue>.GetEnumerator()
        {
            if (this.IsArray)
            {
                return this.Array.Select(v => v.AsInterface).GetEnumerator();
            }
            else if (this.IsDictionary)
            {
                return this.Dictionary.Values.Select(v => v.AsInterface).GetEnumerator();
            }

            IReadOnlyList<Api.IJsonValue> list = System.Array.Empty<Api.IJsonValue>();
            return list.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, Api.IJsonValue>> IEnumerable<KeyValuePair<string, Api.IJsonValue>>.GetEnumerator()
        {
            if (this.IsDictionary)
            {
                return this.Dictionary.Select(pair => new KeyValuePair<string, Api.IJsonValue>(pair.Key, pair.Value.AsInterface)).GetEnumerator();
            }

            IReadOnlyList<KeyValuePair<string, Api.IJsonValue>> list = System.Array.Empty<KeyValuePair<string, Api.IJsonValue>>();
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.IsArray)
            {
                IEnumerable<Api.IJsonValue> enumerable = this;
                IEnumerator<Api.IJsonValue> enumerator = enumerable.GetEnumerator();
                return enumerator;
            }
            else if (this.IsDictionary)
            {
                IEnumerable<KeyValuePair<string, Api.IJsonValue>> enumerable = this;
                IEnumerator<KeyValuePair<string, Api.IJsonValue>> enumerator = enumerable.GetEnumerator();
                return enumerator;
            }

            return System.Array.Empty<Api.IJsonValue>().GetEnumerator();
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.ContainsKey(string key)
        {
            return this.IsDictionary && this.Dictionary.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.TryGetValue(string key, out Api.IJsonValue value)
        {
            if (this.IsDictionary && this.Dictionary.TryGetValue(key, out JsonValue jsonValue))
            {
                value = jsonValue.AsInterface;
                return true;
            }

            value = default;
            return false;
        }

        IEnumerable<string> IReadOnlyDictionary<string, Api.IJsonValue>.Keys => this.IsDictionary ? this.Dictionary.Keys : Enumerable.Empty<string>();
        IEnumerable<Api.IJsonValue> IReadOnlyDictionary<string, Api.IJsonValue>.Values => this.IsDictionary ? this.Dictionary.Values.Select(v => v.AsInterface) : Enumerable.Empty<Api.IJsonValue>();
        int IReadOnlyCollection<KeyValuePair<string, Api.IJsonValue>>.Count => this.IsDictionary ? this.Dictionary.Count : 0;
        int IReadOnlyCollection<Api.IJsonValue>.Count => this.IsArray ? this.Array.Count : (this.IsDictionary ? this.Dictionary.Count : 0);
        Api.IJsonValue IReadOnlyDictionary<string, Api.IJsonValue>.this[string key] => this[key];
        Api.IJsonValue IReadOnlyList<Api.IJsonValue>.this[int index] => this[index];
    }
}
