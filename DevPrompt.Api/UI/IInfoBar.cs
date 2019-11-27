using System;
using System.Windows;

namespace DevPrompt.Api
{
    public interface IInfoBar
    {
        void SetError(Exception exception, string text = null);
        void SetInfo(InfoErrorLevel level, string text, string details = null, FrameworkElement extraContent = null);
        void Clear();
    }

    public enum InfoErrorLevel
    {
        Message,
        Warning,
        Error,
    }
}
