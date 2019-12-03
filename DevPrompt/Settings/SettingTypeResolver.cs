using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace DevPrompt.Settings
{
    internal sealed class SettingTypeResolver : DataContractResolver
    {
        private readonly App app;
        private const string Uri = "dev://DevPrompt.Settings.SettingTypeResolver";

        public SettingTypeResolver(App app)
        {
            this.app = app;
        }

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            Type resolvedType = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            if (resolvedType == null && typeNamespace == SettingTypeResolver.Uri)
            {
                foreach (Assembly assembly in this.app.PluginState.AllPluginAssemblies)
                {
                    resolvedType = assembly.GetType(typeName, throwOnError: false);
                    if (resolvedType != null)
                    {
                        break;
                    }
                }
            }

            return resolvedType;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace))
            {
                return true;
            }

            if (this.app.PluginState.AllPluginAssemblies.Contains(type.Assembly))
            {
                typeNamespace = new XmlDictionaryString(XmlDictionary.Empty, SettingTypeResolver.Uri, 0);
                typeName = new XmlDictionaryString(XmlDictionary.Empty, type.FullName, 0);
                return true;
            }

            return false;
        }
    }
}
