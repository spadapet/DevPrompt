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
    public class PropertyNotifier : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertiesChanging()
        {
            this.OnPropertyChanging(null);
        }

        protected void OnPropertiesChanged()
        {
            this.OnPropertyChanged(null);
        }

        protected void OnPropertyChanging(string name)
        {
            this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
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

            if (name != null)
            {
                this.OnPropertyChanging(name);
            }

            property = value;

            if (name != null)
            {
                this.OnPropertyChanged(name);
            }

            return true;
        }
    }
}
