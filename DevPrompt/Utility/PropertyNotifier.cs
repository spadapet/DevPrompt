using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DevPrompt.Utility
{
    /// <summary>
    /// Base class for all WPF models/view models that notify listeners of property changes
    /// </summary>
    [DataContract]
    public class PropertyNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertiesChanged()
        {
            this.OnPropertyChanged(null);
        }

        protected void OnPropertyChanged(string name)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetPropertyValue<T>(ref T property, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(property, value))
            {
                return false;
            }

            property = value;
            this.OnPropertyChanged(name);
            return true;
        }
    }
}
