using System;

namespace DevPrompt.Utility.Converters
{
    internal sealed class ActiveToBoolConverter : DelegateConverter
    {
        public ActiveToBoolConverter()
            : base(ActiveToBoolConverter.Convert)
        {
        }

        public static object Convert(object value, Type targetType, object parameter)
        {
            if (value is Api.ActiveState state)
            {
                return state == Api.ActiveState.Active;
            }

            throw new InvalidOperationException();
        }
    }
}
