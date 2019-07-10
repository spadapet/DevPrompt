namespace DevPrompt.Plugins
{
    internal enum PluginSourceType
    {
        None,
        BuiltIn, // The EXE itself
        CommandLine, // /plugin on command line, not persisted
        Directory, // Found in search paths
        NuGet, // nuget.org search
    }
}
