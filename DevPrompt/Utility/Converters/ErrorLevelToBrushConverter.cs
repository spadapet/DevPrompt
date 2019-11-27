using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DevPrompt.Utility.Converters
{
    internal sealed class ErrorLevelToBrushConverter : IValueConverter
    {
        public Brush MessageBrush { get; set; }
        public Brush WarningBrush { get; set; }
        public Brush ErrorBrush { get; set; }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Api.InfoErrorLevel level)
            {
                switch (level)
                {
                    case Api.InfoErrorLevel.Message:
                        return this.MessageBrush;

                    case Api.InfoErrorLevel.Warning:
                        return this.WarningBrush;

                    case Api.InfoErrorLevel.Error:
                        return this.ErrorBrush;
                }
            }

            return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }
}
