using System;

namespace DevPrompt.ProcessWorkspace.Utility
{
    internal sealed class VisibleToBoolConverter : DelegateConverter
    {
        public VisibleToBoolConverter()
            : base(VisibleToBoolConverter.Convert)
        {
        }

        private static object Convert(object value, Type targetType, object parameter)
        {
            if (value is Api.ActiveState state)
            {
                return state == Api.ActiveState.Visible;
            }

            throw new InvalidOperationException();
        }
    }
}
