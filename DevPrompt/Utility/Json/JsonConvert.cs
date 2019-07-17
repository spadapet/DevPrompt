using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DevPrompt.Utility.Json
{
    internal class JsonConvert
    {
        public T Convert<T>(Api.IJsonValue value)
        {
            return (T)this.Convert(value, typeof(T));
        }

        public object Convert(Api.IJsonValue value, Type type)
        {
            object result = null;

            if (type == typeof(Api.IJsonValue))
            {
                result = value;
            }
            else if (type == typeof(IEnumerable))
            {
                if (value.IsArray)
                {
                    result = value.Array.Select(v => v.Dynamic);
                }
                else if (value.IsDictionary)
                {
                    result = value.Dictionary.Select(p => new KeyValuePair<string, dynamic>(p.Key, p.Value.Dynamic));
                }
            }
            else if (type == typeof(ICollection) || type == typeof(IList) || type == typeof(ArrayList))
            {
                if (value.IsArray)
                {
                    result = new ArrayList(value.Array.Select(v => v.Dynamic).ToArray());
                }
                else if (value.IsDictionary)
                {
                    result = new ArrayList(value.Dictionary.Select(p => new KeyValuePair<string, dynamic>(p.Key, p.Value.Dynamic)).ToArray());
                }
            }
            else if (type == typeof(Array))
            {
                if (value.IsArray)
                {
                    result = value.Array.Select(v => v.Dynamic).ToArray();
                }
                else if (value.IsDictionary)
                {
                    result = value.Dictionary.Select(p => new KeyValuePair<string, dynamic>(p.Key, p.Value.Dynamic)).ToArray();
                }
            }
            else if (type == typeof(dynamic[]))
            {
                if (value.IsArray)
                {
                    result = value.Array.Select(v => v.Dynamic).ToArray();
                }
            }
            else if (type == typeof(Api.IJsonValue[]))
            {
                if (value.IsArray)
                {
                    result = value.Array.ToArray();
                }
            }
            else if (type == typeof(KeyValuePair<string, dynamic>[]))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.Select(p => new KeyValuePair<string, dynamic>(p.Key, p.Value.Dynamic)).ToArray();
                }
            }
            else if (type == typeof(KeyValuePair<string, Api.IJsonValue>[]))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.ToArray();
                }
            }
            else if (type == typeof(IEnumerable<dynamic>) ||
                type == typeof(ICollection<dynamic>) ||
                type == typeof(IList<dynamic>) ||
                type == typeof(IReadOnlyCollection<dynamic>) ||
                type == typeof(IReadOnlyList<dynamic>) ||
                type == typeof(List<dynamic>))
            {
                if (value.IsArray)
                {
                    result = value.Array.Select(v => v.Dynamic).ToList();
                }
            }
            else if (type == typeof(IEnumerable<Api.IJsonValue>) ||
                type == typeof(ICollection<Api.IJsonValue>) ||
                type == typeof(IList<Api.IJsonValue>) ||
                type == typeof(IReadOnlyCollection<Api.IJsonValue>) ||
                type == typeof(IReadOnlyList<Api.IJsonValue>) ||
                type == typeof(List<Api.IJsonValue>))
            {
                if (value.IsArray)
                {
                    result = value.Array.ToList();
                }
            }
            else if (type == typeof(IEnumerable<KeyValuePair<string, dynamic>>) ||
                type == typeof(ICollection<KeyValuePair<string, dynamic>>) ||
                type == typeof(IList<KeyValuePair<string, dynamic>>) ||
                type == typeof(IReadOnlyCollection<KeyValuePair<string, dynamic>>) ||
                type == typeof(IReadOnlyList<KeyValuePair<string, dynamic>>) ||
                type == typeof(List<KeyValuePair<string, dynamic>>))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.Select(p => new KeyValuePair<string, dynamic>(p.Key, p.Value.Dynamic)).ToList();
                }
            }
            else if (type == typeof(IEnumerable<KeyValuePair<string, Api.IJsonValue>>) ||
                type == typeof(ICollection<KeyValuePair<string, Api.IJsonValue>>) ||
                type == typeof(IList<KeyValuePair<string, Api.IJsonValue>>) ||
                type == typeof(IReadOnlyCollection<KeyValuePair<string, Api.IJsonValue>>) ||
                type == typeof(IReadOnlyList<KeyValuePair<string, Api.IJsonValue>>) ||
                type == typeof(List<KeyValuePair<string, Api.IJsonValue>>))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.ToList();
                }
            }
            else if (type == typeof(IDictionary) ||
                type == typeof(IDictionary<string, dynamic>) ||
                type == typeof(IReadOnlyDictionary<string, dynamic>) ||
                type == typeof(Dictionary<string, dynamic>))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.ToDictionary(p => p.Key, p => p.Value.Dynamic);
                }
            }
            else if (type == typeof(IDictionary<string, Api.IJsonValue>) ||
                type == typeof(IReadOnlyDictionary<string, Api.IJsonValue>) ||
                type == typeof(Dictionary<string, Api.IJsonValue>))
            {
                if (value.IsDictionary)
                {
                    result = value.Dictionary.ToDictionary(p => p.Key, p => p.Value);
                }
            }
            else if (value.IsDictionary)
            {
                result = this.ConvertObject(value.Dictionary, type);
            }
            else if (value.IsNull)
            {
                if (type.IsValueType)
                {
                    JsonConvert.Exception(Resources.JsonConvert_TypeFailed, value, type);
                }
            }
            else  if (!value.IsValid)
            {
                JsonConvert.Exception(Resources.JsonConvert_InvalidValue);
            }
            else
            {
                object rootValue = value.Value;

                if (type.IsAssignableFrom(rootValue.GetType()))
                {
                    result = rootValue;
                }
                else if (rootValue is IConvertible)
                {
                    result = System.Convert.ChangeType(rootValue, type);
                }
            }

            if (result == null)
            {
                if (!value.IsNull)
                {
                    JsonConvert.Exception(Resources.JsonConvert_TypeFailed, value, type);
                }
            }
            else if (!type.IsAssignableFrom(result.GetType()))
            {
                JsonConvert.Exception(Resources.JsonConvert_TypeFailed, value, type);
            }

            return result;
        }

        private object ConvertObject(IReadOnlyDictionary<string, Api.IJsonValue> dict, Type type)
        {
            throw new NotImplementedException();
        }

        private static void Exception(string message, params object[] args)
        {
            throw new JsonException(string.Format(CultureInfo.CurrentCulture, message, args));
        }
    }
}
