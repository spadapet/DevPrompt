using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DevPrompt.Utility.Converters
{
    internal class BrowserIdToInfoConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string id)
            {
                BrowserUtility.BrowserInfo info = new BrowserUtility.BrowserInfo(id, id);

                if (parameter is CollectionViewSource viewSource)
                {
                    info = viewSource.View.OfType<BrowserUtility.BrowserInfo>().Where(i => i.Equals(info)).FirstOrDefault() ?? info;
                }

                return info;
            }

            throw new InvalidOperationException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BrowserUtility.BrowserInfo info)
            {
                return info.Id;
            }

            throw new InvalidOperationException();
        }
    }
}
