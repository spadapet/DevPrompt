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
        Exception,
    }

    [DebuggerTypeProxy(typeof(DebuggerView))]
    internal struct JsonValue
        : Api.IJsonValue
        , IEquatable<JsonValue>
        , IReadOnlyList<Api.IJsonValue>
        , IReadOnlyDictionary<string, Api.IJsonValue>
    {
        private readonly JsonValueType type;
        private readonly JsonValueContext context;
        private readonly JsonToken token;
        private readonly object value;

        public JsonValue(JsonValueType type, JsonToken token, JsonValueContext context)
        {
            this.type = type;
            this.context = context;
            this.token = token;
            this.value = null;
        }

        public JsonValue(JsonValueType type, object value, JsonValueContext context)
            : this()
        {
            this.type = type;
            this.context = context;
            this.token = (value is JsonException ex) ? ex.ErrorToken : default;
            this.value = value;
        }

        public override int GetHashCode()
        {
            return (this.value != null)
                ? this.value.GetHashCode()
                : HashUtility.HashSubstring(this.context.Json, this.token.Start, this.token.Length);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonValue other && this.Equals(other);
        }

        public bool Equals(JsonValue other)
        {
            if (object.ReferenceEquals(this.value, other.value) &&
                object.ReferenceEquals(this.context, other.context) &&
                this.token.Length == other.token.Length)
            {
                return (this.context == null) || string.CompareOrdinal(
                    this.context.Json,
                    this.token.Start,
                    other.context.Json,
                    other.token.Start,
                    this.token.Length) == 0;
            }

            return false;
        }

        public static bool operator ==(JsonValue x, JsonValue y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(JsonValue x, JsonValue y)
        {
            return !x.Equals(y);
        }

        public bool IsType(JsonValueType type) => this.type == type;
        private void CheckType(JsonValueType type)
        {
            if (!this.IsType(type))
            {
                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, type));
            }
        }

        bool Api.IJsonValue.IsArray => this.IsType(JsonValueType.Array);
        bool Api.IJsonValue.IsBool => this.IsType(JsonValueType.Bool);
        bool Api.IJsonValue.IsDictionary => this.IsType(JsonValueType.Dictionary);
        bool Api.IJsonValue.IsDouble => this.IsNumber && double.TryParse(this.Text, out _);
        bool Api.IJsonValue.IsException => this.IsType(JsonValueType.Exception);
        bool Api.IJsonValue.IsInt => this.IsNumber && int.TryParse(this.Text, out _);
        bool Api.IJsonValue.IsNull => this.IsType(JsonValueType.Null);
        bool Api.IJsonValue.IsString => this.IsType(JsonValueType.String);
        bool Api.IJsonValue.IsUnset => this.IsType(JsonValueType.Unset);
        private bool IsNumber => this.IsType(JsonValueType.Number);

        IReadOnlyList<Api.IJsonValue> Api.IJsonValue.Array => (IReadOnlyList<Api.IJsonValue>)this.AsInterface;
        IReadOnlyDictionary<string, Api.IJsonValue> Api.IJsonValue.Dictionary => (IReadOnlyDictionary<string, Api.IJsonValue>)this.AsInterface;
        private IReadOnlyList<JsonValue> InternalArray => (IReadOnlyList<JsonValue>)this.value;
        private IReadOnlyDictionary<string, JsonValue> InternalDictionary => (IReadOnlyDictionary<string, JsonValue>)this.value;
        private JsonValue Unset => new JsonValue(JsonValueType.Unset, default(JsonToken), this.context);
        private Api.IJsonValue AsInterface => this.context.GetInterface(this);
        private string Text => this.token.GetText(this.context.Json);

        bool Api.IJsonValue.Bool
        {
            get
            {
                this.CheckType(JsonValueType.Bool);
                return this.token.Type == JsonTokenType.True;
            }
        }

        int Api.IJsonValue.Int
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

        double Api.IJsonValue.Double
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

        string Api.IJsonValue.String
        {
            get
            {
                this.CheckType(JsonValueType.String);
                return this.token.GetDecodedString(this.context.Json);
            }
        }

        Api.IJsonException Api.IJsonValue.Exception
        {
            get
            {
                if (this.IsType(JsonValueType.Exception) && this.value is Api.IJsonException ex)
                {
                    return ex;
                }

                throw new JsonException(this.token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, JsonValueType.Exception));
            }
        }

        object Api.IJsonValue.Value
        {
            get
            {
                Api.IJsonValue value = this;

                switch (this.type)
                {
                    case JsonValueType.Bool:
                        return value.Bool;

                    case JsonValueType.Number:
                        return value.Double;

                    case JsonValueType.String:
                        return value.String;

                    case JsonValueType.Array:
                    case JsonValueType.Dictionary:
                    case JsonValueType.Exception:
                        return this.value;

                    default:
                        return null;
                }
            }
        }

        Api.IJsonValue Api.IJsonValue.this[string path] => this.GetFromPath(path, 0).AsInterface;
        Api.IJsonValue Api.IJsonValue.this[int index] => this.GetFromIndex(index).AsInterface;

        private JsonValue GetFromIndex(int index)
        {
            if (this.IsType(JsonValueType.Array) && index >= 0 && index < this.InternalArray.Count)
            {
                return this.InternalArray[index];
            }

            return this.Unset;
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
                    return this.GetFromIndex(result).GetFromPath(path, endPos + 1);
                }
            }
            else if (this.IsType(JsonValueType.Dictionary))
            {
                if (pos > 0)
                {
                    if (path[pos] == '.')
                    {
                        pos++;
                    }
                    else
                    {
                        return this.Unset;
                    }
                }

                int endPos = path.IndexOfAny(".[".ToCharArray(), pos);
                if (endPos == -1)
                {
                    endPos = path.Length;
                }

                string name = path.Substring(pos, endPos - pos);
                if (this.InternalDictionary.TryGetValue(name, out JsonValue result))
                {
                    return result.GetFromPath(path, endPos);
                }
            }

            return this.Unset;
        }

        /// <summary>
        /// Just for debugger display
        /// </summary>
        public override string ToString()
        {
            Api.IJsonValue value = this;

            switch (this.type)
            {
                case JsonValueType.Unset:
                    return "<unset>";

                case JsonValueType.Null:
                case JsonValueType.Bool:
                case JsonValueType.Number:
                    return this.Text;

                case JsonValueType.String:
                    return value.String;

                case JsonValueType.Array:
                    return $"Array, Count={this.InternalArray.Count}";

                case JsonValueType.Dictionary:
                    return $"Dictionary, Count={this.InternalDictionary.Count}";

                case JsonValueType.Exception:
                    return $"Exception, {value.Exception.Message}";

                default:
                    return null;
            }
        }

        IEnumerator<Api.IJsonValue> IEnumerable<Api.IJsonValue>.GetEnumerator()
        {
            if (this.IsType(JsonValueType.Array))
            {
                return this.InternalArray.Select(v => v.AsInterface).GetEnumerator();
            }

            IReadOnlyList<Api.IJsonValue> list = Array.Empty<Api.IJsonValue>();
            return list.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, Api.IJsonValue>> IEnumerable<KeyValuePair<string, Api.IJsonValue>>.GetEnumerator()
        {
            if (this.IsType(JsonValueType.Dictionary))
            {
                return this.InternalDictionary.Select(pair => new KeyValuePair<string, Api.IJsonValue>(pair.Key, pair.Value.AsInterface)).GetEnumerator();
            }

            IReadOnlyList<KeyValuePair<string, Api.IJsonValue>> list = Array.Empty<KeyValuePair<string, Api.IJsonValue>>();
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.IsType(JsonValueType.Array))
            {
                IEnumerable<Api.IJsonValue> enumerable = this;
                IEnumerator<Api.IJsonValue> enumerator = enumerable.GetEnumerator();
                return enumerator;
            }
            else if (this.IsType(JsonValueType.Dictionary))
            {
                IEnumerable<KeyValuePair<string, Api.IJsonValue>> enumerable = this;
                IEnumerator<KeyValuePair<string, Api.IJsonValue>> enumerator = enumerable.GetEnumerator();
                return enumerator;
            }

            return Array.Empty<Api.IJsonValue>().GetEnumerator();
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.ContainsKey(string key)
        {
            return this.IsType(JsonValueType.Dictionary) && this.InternalDictionary.ContainsKey(key);
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.TryGetValue(string key, out Api.IJsonValue value)
        {
            if (this.IsType(JsonValueType.Dictionary) && this.InternalDictionary.TryGetValue(key, out JsonValue jsonValue))
            {
                value = jsonValue.AsInterface;
                return true;
            }

            value = this.Unset.AsInterface;
            return false;
        }

        IEnumerable<string> IReadOnlyDictionary<string, Api.IJsonValue>.Keys => this.IsType(JsonValueType.Dictionary) ? this.InternalDictionary.Keys : Enumerable.Empty<string>();
        IEnumerable<Api.IJsonValue> IReadOnlyDictionary<string, Api.IJsonValue>.Values => this.IsType(JsonValueType.Dictionary) ? this.InternalDictionary.Values.Select(v => v.AsInterface) : Enumerable.Empty<Api.IJsonValue>();
        int IReadOnlyCollection<KeyValuePair<string, Api.IJsonValue>>.Count => this.IsType(JsonValueType.Dictionary) ? this.InternalDictionary.Count : 0;
        int IReadOnlyCollection<Api.IJsonValue>.Count => this.IsType(JsonValueType.Array) ? this.InternalArray.Count : 0;
        Api.IJsonValue IReadOnlyDictionary<string, Api.IJsonValue>.this[string key] => this.GetFromPath(key, 0).AsInterface;
        Api.IJsonValue IReadOnlyList<Api.IJsonValue>.this[int index] => this.GetFromIndex(index).AsInterface;

        private class DebuggerView
        {
            private JsonValue value;

            public DebuggerView(JsonValue value)
            {
                this.value = value;
            }

            public JsonValueType Type => this.value.type;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value => this.value.AsInterface.Value;
        }
    }
}
