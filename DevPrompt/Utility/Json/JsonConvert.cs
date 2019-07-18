using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DevPrompt.Utility.Json
{
    internal class JsonConvert
    {
        private ConcurrentDictionary<Type, CachedTypeInfo> typeInfos;
        private ConcurrentDictionary<Type, CachedCollectionInterfaceInfo> collectionInterfaceInfos;
        private ConcurrentDictionary<Type, CachedCollectionInterfaceInfo[]> typeToCollections;

        public JsonConvert()
        {
            this.typeInfos = new ConcurrentDictionary<Type, CachedTypeInfo>();
            this.collectionInterfaceInfos = new ConcurrentDictionary<Type, CachedCollectionInterfaceInfo>();
            this.typeToCollections = new ConcurrentDictionary<Type, CachedCollectionInterfaceInfo[]>();
        }

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
            else if (!value.IsValid)
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
                    result = System.Convert.ChangeType(rootValue, type, CultureInfo.InvariantCulture);
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
            object result = Activator.CreateInstance(type);
            CachedTypeInfo info = this.GetTypeInfo(type);

            foreach (KeyValuePair<string, Api.IJsonValue> pair in dict)
            {
                if (info.TryFindMember(pair.Key, out CachedMemberInfo memberInfo))
                {
                    memberInfo.Set(result, pair.Value);
                }
            }

            return result;
        }

        private CachedTypeInfo GetTypeInfo(Type type)
        {
            return this.typeInfos.GetOrAdd(type, t => new CachedTypeInfo(this, t));
        }

        private CachedCollectionInterfaceInfo GetCollectionInterfaceInfo(Type type)
        {
            return this.collectionInterfaceInfos.GetOrAdd(type, t => new CachedCollectionInterfaceInfo(t));
        }

        private CachedCollectionInterfaceInfo[] GetCollectionInterfacesForType(Type type)
        {
            return this.typeToCollections.GetOrAdd(type, newType =>
            {
                List<CachedCollectionInterfaceInfo> interfaceInfos = new List<CachedCollectionInterfaceInfo>();

                IEnumerable<Type> interfaceTypes = newType.GetInterfaces();
                if (type.IsInterface)
                {
                    interfaceTypes = interfaceTypes.Append(type);
                }

                foreach (Type interfaceType in interfaceTypes)
                {
                    // These interfaces need an "Add" method
                    if (typeof(IList).IsAssignableFrom(interfaceType) || (interfaceType.IsGenericType && typeof(ICollection<>) == interfaceType.GetGenericTypeDefinition()))
                    {
                        CachedCollectionInterfaceInfo interfaceInfo = this.GetCollectionInterfaceInfo(interfaceType);
                        if (interfaceInfo.Parameters.Length == 1)
                        {
                            interfaceInfos.Add(interfaceInfo);
                        }
                    }
                }

                return (interfaceInfos.Count > 0) ? interfaceInfos.ToArray() : Array.Empty<CachedCollectionInterfaceInfo>();
            });
        }

        private static void Exception(string message, params object[] args)
        {
            throw new JsonException(string.Format(CultureInfo.CurrentCulture, message, args));
        }

        private class CachedTypeInfo
        {
            public Type Type { get; }
            public CachedMemberInfo[] Members { get; }
            private Dictionary<string, CachedMemberInfo> nameToMember;

            public CachedTypeInfo(JsonConvert owner, Type type)
            {
                this.Type = type;
                this.Members = type.FindMembers(MemberTypes.Field | MemberTypes.Property,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    this.MemberFilter, null).Select(m => this.CreateMemberInfo(owner, m)).Where(m => m != null).ToArray();

                this.nameToMember = this.Members.ToDictionary(i => i.Name, i => i);
            }

            public bool TryFindMember(string name, out CachedMemberInfo info)
            {
                return this.nameToMember.TryGetValue(name, out info);
            }

            private bool MemberFilter(MemberInfo info, object obj)
            {
                return true;
            }

            private CachedMemberInfo CreateMemberInfo(JsonConvert owner, MemberInfo info)
            {
                CachedMemberInfo cachedInfo = null;

                if (info is FieldInfo fieldInfo)
                {
                    cachedInfo = new CachedFieldInfo(owner, fieldInfo);
                }
                else if (info is PropertyInfo propertyInfo)
                {
                    cachedInfo = new CachedPropertyInfo(owner, propertyInfo);
                }

                if (cachedInfo != null && cachedInfo.Kind == CachedMemberKind.None)
                {
                    cachedInfo = null;
                }

                return cachedInfo;
            }
        }

        private enum CachedMemberKind
        {
            None,
            SetValue,
            AddToCollection,
        }

        private abstract class CachedMemberInfo
        {
            public abstract Type ValueType { get; }
            public abstract string Name { get; }
            public abstract CachedMemberKind Kind { get; }

            public abstract void Set(object target, Api.IJsonValue value);

            protected void AddToCollection(object collection, CachedCollectionInterfaceInfo[] interfaces, Api.IJsonValue value)
            {
                if (!value.IsArray)
                {
                    throw new JsonException(Resources.JsonConvert_ExpectedArray);
                }

                CachedCollectionInterfaceInfo interfaceInfo = interfaces.First(i => i.Parameters.Length == 1);
                Type paramType = interfaceInfo.Parameters[0].ParameterType;

                foreach (Api.IJsonValue childValue in value.Array)
                {
                    object setValue = childValue.Convert(paramType);
                    interfaceInfo.AddMethod.Invoke(collection, new object[] { setValue });
                }
            }
        }

        private class CachedFieldInfo : CachedMemberInfo
        {
            public FieldInfo FieldInfo { get; }
            public override Type ValueType { get; }
            public override string Name { get; }
            public override CachedMemberKind Kind { get; }
            private CachedCollectionInterfaceInfo[] collectionInterfaces;

            public CachedFieldInfo(JsonConvert owner, FieldInfo info)
            {
                this.FieldInfo = info;
                this.ValueType = info.FieldType;
                this.Name = info.Name;
                this.collectionInterfaces = owner.GetCollectionInterfacesForType(info.FieldType);
                this.Kind = (this.collectionInterfaces.Length > 0) ? CachedMemberKind.AddToCollection : CachedMemberKind.SetValue;
            }

            public override void Set(object target, Api.IJsonValue value)
            {
                if (this.Kind == CachedMemberKind.SetValue)
                {
                    object setValue = value.Convert(this.ValueType);
                    this.FieldInfo.SetValue(target, setValue);
                }
                else if (this.Kind == CachedMemberKind.AddToCollection)
                {
                    object collection = this.FieldInfo.GetValue(target);
                    this.AddToCollection(collection, this.collectionInterfaces, value);
                }
            }
        }

        private class CachedPropertyInfo : CachedMemberInfo
        {
            public PropertyInfo PropertyInfo { get; }
            public override Type ValueType { get; }
            public override string Name { get; }
            public override CachedMemberKind Kind { get; }
            private MethodInfo getMethod;
            private MethodInfo setMethod;
            private CachedCollectionInterfaceInfo[] collectionInterfaces;

            public CachedPropertyInfo(JsonConvert owner, PropertyInfo info)
            {
                this.PropertyInfo = info;
                this.ValueType = info.PropertyType;
                this.Name = info.Name;
                this.Kind = CachedMemberKind.None;

                this.getMethod = this.PropertyInfo.GetGetMethod(nonPublic: false);
                this.setMethod = this.PropertyInfo.GetSetMethod(nonPublic: false);

                if (this.setMethod != null)
                {
                    this.Kind = CachedMemberKind.SetValue;
                    this.collectionInterfaces = Array.Empty<CachedCollectionInterfaceInfo>();
                }
                else if (this.getMethod != null)
                {
                    this.collectionInterfaces = owner.GetCollectionInterfacesForType(info.PropertyType);
                    this.Kind = (this.collectionInterfaces.Length > 0) ? CachedMemberKind.AddToCollection : CachedMemberKind.SetValue;
                }
            }

            public override void Set(object target, Api.IJsonValue value)
            {
                if (this.Kind == CachedMemberKind.SetValue)
                {
                    object setValue = value.Convert(this.ValueType);
                    this.setMethod.Invoke(target, new object[] { setValue });
                }
                else if (this.Kind == CachedMemberKind.AddToCollection)
                {
                    object collection = this.getMethod.Invoke(target, Array.Empty<object>());
                    this.AddToCollection(collection, this.collectionInterfaces, value);
                }
            }
        }

        private class CachedCollectionInterfaceInfo
        {
            public Type Type { get; }
            public MethodInfo AddMethod { get; }
            public ParameterInfo[] Parameters { get; }

            public CachedCollectionInterfaceInfo(Type type)
            {
                this.Type = type;
                this.AddMethod = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod);
                this.Parameters = this.AddMethod?.GetParameters() ?? Array.Empty<ParameterInfo>();

                Debug.Assert(this.AddMethod != null, $"Interface '{type}' missing an 'Add' method");
            }
        }
    }
}
