using System;

namespace DevPrompt.Utility
{
    public class DelegateDisposable : IDisposable
    {
        private Action disposeAction;

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
