namespace DevPrompt.Update
{
    internal enum WorkerStateType
    {
        None,
        ClosingWindows,
        DownloadingHeaders,
        Downloading,
        Extracting,
        DoneSuccess,
        DoneCancel,
        DoneException,
    }
}
