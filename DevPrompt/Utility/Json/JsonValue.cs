using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// Heap copy of JsonValue that allows dynamic binding too
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    [DebuggerDisplay("{this.value}")]
    internal class JsonValue
        : DynamicObject
        , Api.IJsonValue
        , IReadOnlyList<Api.IJsonValue>
        , IReadOnlyDictionary<string, Api.IJsonValue>
    {
        private JsonValueData value;

        public JsonValue(JsonValueData value)
        {
            this.value = value;
        }

        private bool IsType(JsonValueType type) => this.value.Type == type;
        private JsonException WrongType(JsonValueType type) => new JsonException(this.value.Token, string.Format(CultureInfo.CurrentCulture, Resources.JsonValue_WrongType, type));

        bool Api.IJsonValue.IsArray => this.IsType(JsonValueType.Array);
        bool Api.IJsonValue.IsBool => this.IsType(JsonValueType.Bool);
        bool Api.IJsonValue.IsDictionary => this.IsType(JsonValueType.Dictionary);
        bool Api.IJsonValue.IsDouble => this.IsType(JsonValueType.Number) && double.TryParse(this.value.ToString(), out _);
        bool Api.IJsonValue.IsInt => this.IsType(JsonValueType.Number) && int.TryParse(this.value.ToString(), out _);
        bool Api.IJsonValue.IsNull => this.IsType(JsonValueType.Null);
        bool Api.IJsonValue.IsString => this.IsType(JsonValueType.String);
        bool Api.IJsonValue.IsValid => !this.IsType(JsonValueType.None);

        IReadOnlyList<Api.IJsonValue> Api.IJsonValue.Array => this;
        bool Api.IJsonValue.Bool => this.IsType(JsonValueType.Bool) ? this.value.Token.Type == JsonTokenType.True : throw this.WrongType(JsonValueType.Bool);
        IReadOnlyDictionary<string, Api.IJsonValue> Api.IJsonValue.Dictionary => this;
        double Api.IJsonValue.Double => (this.IsType(JsonValueType.Number) && double.TryParse(this.value.ToString(), out double value)) ? value : throw this.WrongType(JsonValueType.Number);
        int Api.IJsonValue.Int => (this.IsType(JsonValueType.Number) && int.TryParse(this.value.ToString(), out int value)) ? value : throw this.WrongType(JsonValueType.Number);
        string Api.IJsonValue.String => this.IsType(JsonValueType.String) ? JsonTokenizer.DecodeString(this.value.Context.Json, this.value.Token) : throw this.WrongType(JsonValueType.String);

        Api.IJsonValue Api.IJsonValue.this[int index] => this.value.List.Array[index].Value;
        Api.IJsonValue Api.IJsonValue.this[string key] => this.value.List.Dictionary[key].Value;

        object Api.IJsonValue.Value
        {
            get
            {
                Api.IJsonValue value = this;

                switch (this.value.Type)
                {
                    case JsonValueType.Bool:
                        return value.Bool;

                    case JsonValueType.Number:
                        if (value.IsInt)
                        {
                            return value.Int;
                        }
                        else if (value.IsDouble)
                        {
                            return value.Double;
                        }
                        else if (decimal.TryParse(this.value.ToString(), out decimal decimalValue))
                        {
                            return decimalValue;
                        }
                        break;

                    case JsonValueType.String:
                        return value.String;

                    case JsonValueType.Array:
                        return this.value.List.Array;

                    case JsonValueType.Dictionary:
                        return this.value.List.Dictionary;
                }

                return null;
            }
        }

        IEnumerator<Api.IJsonValue> IEnumerable<Api.IJsonValue>.GetEnumerator()
        {
            return this.value.List.Array.Select(v => v.Value).GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, Api.IJsonValue>> IEnumerable<KeyValuePair<string, Api.IJsonValue>>.GetEnumerator()
        {
            return this.value.List.Dictionary.Select(p => new KeyValuePair<string, Api.IJsonValue>(p.Key, p.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerator enumerator;

            if (this.IsType(JsonValueType.Array))
            {
                IEnumerable<Api.IJsonValue> enumerable = this;
                enumerator = enumerable.GetEnumerator();
            }
            else if (this.IsType(JsonValueType.Dictionary))
            {
                IEnumerable<KeyValuePair<string, Api.IJsonValue>> enumerable = this;
                enumerator = enumerable.GetEnumerator();
            }
            else
            {
                enumerator = System.Array.Empty<Api.IJsonValue>().GetEnumerator();
            }

            return enumerator;
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.TryGetValue(string key, out Api.IJsonValue value)
        {
            if (this.value.List.Dictionary.TryGetValue(key, out JsonValueData data))
            {
                value = data.Value;
                return true;
            }

            value = null;
            return false;
        }

        bool IReadOnlyDictionary<string, Api.IJsonValue>.ContainsKey(string key) => this.value.List.Dictionary.ContainsKey(key);
        IEnumerable<string> IReadOnlyDictionary<string, Api.IJsonValue>.Keys => this.value.List.Dictionary.Keys;
        IEnumerable<Api.IJsonValue> IReadOnlyDictionary<string, Api.IJsonValue>.Values => this.value.List.Dictionary.Values.Select(v => v.Value);
        int IReadOnlyCollection<KeyValuePair<string, Api.IJsonValue>>.Count => this.value.List.Dictionary.Count;
        int IReadOnlyCollection<Api.IJsonValue>.Count => this.value.List.Array.Count;
        Api.IJsonValue IReadOnlyDictionary<string, Api.IJsonValue>.this[string key] => this.value.List.Dictionary[key].Value;
        Api.IJsonValue IReadOnlyList<Api.IJsonValue>.this[int index] => this.value.List.Array[index].Value;

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = ((Api.IJsonValue)this).Value;
            if (result != null && !binder.Type.IsAssignableFrom(result.GetType()))
            {
                if (result is IConvertible convertible)
                {
                    try
                    {
                        result = Convert.ChangeType(result, binder.Type, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        result = null;
                    }
                }
                else
                {
                    result = null;
                }
            }

            return result != null;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            Api.IJsonValue currentValue = this;

            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] is int intIndex)
                {
                    currentValue = currentValue[intIndex];
                }
                else if (indexes[i] is string stringIndex)
                {
                    currentValue = currentValue[stringIndex];
                }
                else
                {
                    // Force an invalid value
                    currentValue = currentValue[-1];
                }
            }

            result = currentValue;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = ((Api.IJsonValue)this)[binder.Name];
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((Api.IJsonValue)this).Dictionary.Keys;
        }

        private class DebuggerView
        {
            private JsonValue value;

            public DebuggerView(JsonValue value)
            {
                this.value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value => ((Api.IJsonValue)this.value).Value;
        }
    }
}
