using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace DevPrompt.Utility.Json
{
    /// <summary>
    /// Wrapper for IJsonValue that allows dynamic binding and conversion
    /// </summary>
    [DebuggerTypeProxy(typeof(DebuggerView))]
    [DebuggerDisplay("{this.value}")]
    internal class JsonDynamic : DynamicObject
    {
        private Api.IJsonValue value;

        public JsonDynamic(JsonValue value)
        {
            this.value = value;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            return this.value.TryConvert(binder.Type, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            Api.IJsonValue currentValue = this.value;

            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] is int intIndex)
                {
                    currentValue = currentValue[intIndex];
                }
                else if (indexes[i] is string stringIndex)
                {
                    if (int.TryParse(stringIndex, out int intIndex2))
                    {
                        currentValue = currentValue[intIndex2];
                    }
                    else
                    {
                        currentValue = currentValue[stringIndex];
                    }
                }
                else
                {
                    // Force an invalid value
                    currentValue = currentValue[-1];
                }
            }

            result = currentValue.IsValid ? currentValue.Dynamic : null;
            return result != null;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            Api.IJsonValue value = this.value[binder.Name];
            result = value.IsValid ? value.Dynamic : null;
            return value.IsNull || result != null;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.value.Dictionary.Keys;
        }

        private class DebuggerView
        {
            private JsonDynamic value;

            public DebuggerView(JsonDynamic value)
            {
                this.value = value;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public object Value => this.value.value;
        }
    }
}
