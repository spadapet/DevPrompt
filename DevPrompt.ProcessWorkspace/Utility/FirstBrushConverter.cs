using System;
using System.Linq;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.Utility
{
    internal sealed class FirstBrushConverter : DelegateMultiConverter
    {
        public FirstBrushConverter()
            : base(FirstBrushConverter.Convert)
        {
        }

        private static object Convert(object[] values, Type targetType, object parameter)
        {
            return values.OfType<Brush>().FirstOrDefault();
        }
    }
}
