using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// Stores child JSON values in either an array or dictionary after parsing them
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    internal class JsonValues : IReadOnlyList<JsonValueData>, IReadOnlyDictionary<string, JsonValueData>
    {
        public JsonContext Context { get; }
        private int[] keyHashes;
        private string[] keys;
        private JsonValueData[] values;

        public JsonValues(List<JsonValueData> values, JsonContext context)
        {
            this.keyHashes = System.Array.Empty<int>();
            this.keys = System.Array.Empty<string>();
            this.values = new JsonValueData[values.Count];
            this.Context = context;

            values.CopyTo(this.values);
        }

        public JsonValues(Dictionary<string, JsonValueData> values, JsonContext context)
        {
            this.keyHashes = new int[values.Count];
            this.keys = new string[values.Count];
            this.values = new JsonValueData[values.Count];
            this.Context = context;

            values.Values.CopyTo(this.values, 0);
            values.Keys.CopyTo(this.keys, 0);

            for (int i = 0; i < this.keys.Length; i++)
            {
                this.keyHashes[i] = this.keys[i].GetHashCode();
            }
        }

        private bool IsArray => object.ReferenceEquals(this.keyHashes, System.Array.Empty<int>());
        public IReadOnlyList<JsonValueData> Array => this;
        public IReadOnlyDictionary<string, JsonValueData> Dictionary => this;

        JsonValueData IReadOnlyList<JsonValueData>.this[int index] => (index >= 0 && index < this.values.Length) ? this.values[index] : JsonValueData.None(this.Context);
        int IReadOnlyCollection<JsonValueData>.Count => this.values.Length;
        IEnumerator<JsonValueData> IEnumerable<JsonValueData>.GetEnumerator() => ((IList<JsonValueData>)this.values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.IsArray ? (IEnumerator)this.Array.GetEnumerator() : (IEnumerator)this.Dictionary.GetEnumerator();

        int IReadOnlyCollection<KeyValuePair<string, JsonValueData>>.Count => this.keys.Length;
        IEnumerable<string> IReadOnlyDictionary<string, JsonValueData>.Keys => this.keys;
        IEnumerable<JsonValueData> IReadOnlyDictionary<string, JsonValueData>.Values => this.values;
        bool IReadOnlyDictionary<string, JsonValueData>.ContainsKey(string key) => this.IndexOfKey(key) != -1;

        IEnumerator<KeyValuePair<string, JsonValueData>> IEnumerable<KeyValuePair<string, JsonValueData>>.GetEnumerator()
        {
            for (int i = 0; i < this.keys.Length; i++)
            {
                yield return new KeyValuePair<string, JsonValueData>(this.keys[i], this.values[i]);
            }
        }

        bool IReadOnlyDictionary<string, JsonValueData>.TryGetValue(string key, out JsonValueData value)
        {
            int i = this.IndexOfKey(key);
            value = (i != -1) ? this.values[i] : JsonValueData.None(this.Context);
            return i != -1;
        }

        JsonValueData IReadOnlyDictionary<string, JsonValueData>.this[string key]
        {
            get
            {
                int i = this.IndexOfKey(key);
                return (i != -1) ? this.values[i] : JsonValueData.None(this.Context);
            }
        }

        private int IndexOfKey(string key)
        {
            if (key != null)
            {
                int hash = key.GetHashCode();
                for (int i = 0; i < this.keyHashes.Length; i++)
                {
                    if (this.keyHashes[i] == hash && this.keys[i] == key)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public override string ToString()
        {
            return this.IsArray
                ? $"Array, Count={this.Array.Count}"
                : $"Dictionary, Count={this.Dictionary.Count}";
        }

        private class DebuggerView
        {
            private JsonValues values;

            public DebuggerView(JsonValues values)
            {
                this.values = values;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public IEnumerable Values => this.values.IsArray
                ? (IEnumerable)this.values.Array.ToArray()
                : (IEnumerable)this.values.Dictionary.ToArray();
        }
    }
}
