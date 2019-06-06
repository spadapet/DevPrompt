using System;
using System.Reflection;
using System.Windows.Media;

namespace DevPrompt.Api
{
    public interface IPluginInfo
    {
        Assembly Assembly { get; }
        string Name { get; }
        string Description { get; }
        string CreatedBy { get; }
        string Version { get; }
        string ReleaseNotes { get; }
        Uri MoreInfoLink { get; }
        ImageSource Icon { get; }
    }
}
