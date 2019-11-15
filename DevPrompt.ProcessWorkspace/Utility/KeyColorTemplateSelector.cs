using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevPrompt.ProcessWorkspace.Utility
{
    public sealed class KeyColorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate ColorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Color color && color == default)
            {
                return this.DefaultTemplate;
            }

            return this.ColorTemplate;
        }
    }
}
