using System;

namespace DevPrompt.Api
{
    public interface IProgressBar
    {
        IDisposable Begin(Action cancelAction, string text);
    }
}
