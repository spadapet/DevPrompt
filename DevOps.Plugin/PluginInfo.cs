using DevPrompt.Api;
using System;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace DevOps
{
    [Export(typeof(IPluginInfo))]
    public class PluginInfo : IPluginInfo
    {
        public Assembly Assembly => Assembly.GetExecutingAssembly();
        public string Name => PluginInfo.FirstAssemblyAttribute<AssemblyTitleAttribute>()?.Title ?? string.Empty;
        public string Description => PluginInfo.FirstAssemblyAttribute<AssemblyDescriptionAttribute>()?.Description ?? string.Empty;
        public string CreatedBy => PluginInfo.FirstAssemblyAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public string ReleaseNotes => string.Empty;
        public Uri MoreInfoLink => null;
        public ImageSource Icon => null;

        public PluginInfo()
        {
        }

        private static T FirstAssemblyAttribute<T>() where T : Attribute
        {
            return Assembly.GetExecutingAssembly().GetCustomAttributes<T>().FirstOrDefault();
        }
    }
}
