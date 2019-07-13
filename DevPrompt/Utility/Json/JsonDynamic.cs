using System;
using System.Collections.Generic;
using System.Dynamic;

namespace DevPrompt.Utility.Json
{
    internal class JsonDynamic : DynamicObject
    {
        private JsonValue value;

        public JsonDynamic(JsonValue value)
        {
            this.value = value;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (this.value.IsDictionary)
            {
                return this.value.Dictionary.Keys;
            }

            return base.GetDynamicMemberNames();
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (JsonDynamic.TryCast(this.value, binder.Type, out result))
            {
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            JsonValue value = this.value;

            for (int i = 0; i < indexes.Length && !value.IsUnset; i++)
            {
                if (indexes[i] is int index)
                {
                    value = value[index];
                }
                else if (indexes[i] is string path)
                {
                    value = value[path];
                }
            }

            if (JsonDynamic.TryCast(value, binder.ReturnType, out result))
            {
                return true;
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            JsonValue value = this.value[binder.Name];
            if (JsonDynamic.TryCast(value, binder.ReturnType, out result))
            {
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        private static bool TryCast(JsonValue value, Type type, out object result)
        {
            result = null;

            if (type.IsAssignableFrom(typeof(object)))
            {
                result = value.Value;
            }
            else if (value.IsBool)
            {
                if (type.IsAssignableFrom(typeof(bool)))
                {
                    result = value.Bool;
                }
            }
            else if (value.IsNumber)
            {
                if (type.IsAssignableFrom(typeof(int)))
                {
                    result = value.Int;
                }
                else if (type.IsAssignableFrom(typeof(double)))
                {
                    result = value.Double;
                }
            }
            else if (value.IsString)
            {
                if (type.IsAssignableFrom(typeof(string)))
                {
                    result = value.String;
                }
            }

            return result != null;
        }
    }
}
