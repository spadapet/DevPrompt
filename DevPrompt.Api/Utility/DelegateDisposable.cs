using System;

namespace DevPrompt.Api.Utility
{
    /// <summary>
    /// Just calls a delegate when Dispose() is called
    /// </summary>
    public sealed class DelegateDisposable : IDisposable
    {
        private readonly Action disposeAction;

        public DelegateDisposable(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        void IDisposable.Dispose()
        {
            this.disposeAction?.Invoke();
        }
    }
}
