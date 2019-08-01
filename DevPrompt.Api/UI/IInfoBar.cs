using System;

namespace DevPrompt.Api
{
    public interface IInfoBar
    {
        void SetError(Exception exception, string text = null);
        void Clear();
    }
}
