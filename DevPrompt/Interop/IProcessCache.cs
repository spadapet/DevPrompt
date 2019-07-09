namespace DevPrompt.Interop
{
    /// <summary>
    /// Maps between the native IProcess and the managed NativeProcess wrapper.
    /// NativeProcess exposes Api.IProcess for managed plugins to use.
    /// [Import] this interface to get the one global cache.
    /// </summary>
    internal interface IProcessCache
    {
        NativeProcess GetNativeProcess(IProcess process);
        bool RemoveNativeProcess(IProcess process);
    }
}
