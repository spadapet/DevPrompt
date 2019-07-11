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
            if (this.value.IsBool)
            {
            }
            else if (this.value.IsNumber)
            {
            }
            else if (this.value.IsString)
            {
            }

            return base.TryConvert(binder, out result);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (this.value.IsArray)
            {
            }

            return base.TryGetIndex(binder, indexes, out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this.value.IsDictionary)
            {
            }

            return base.TryGetMember(binder, out result);
        }
    }
}
