using DevPrompt.ProcessWorkspace.Utility;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DevPrompt.UI.ViewModels
{
    internal class PluginSortVM : PropertyNotifier
    {
        public string Name { get; }
        public IComparer Comparer { get; }

        public PluginSortVM(string name, IComparer comparer)
        {
            this.Name = name;
            this.Comparer = comparer;
        }
    }

    internal class PluginSortInstalled : IComparer, IComparer<IPluginVM>
    {
        int IComparer.Compare(object x, object y)
        {
            return this.Compare(x as IPluginVM, y as IPluginVM);
        }

        public int Compare(IPluginVM x, IPluginVM y)
        {
            if (x != null)
            {
                if (y != null)
                {
                    if (x.State.HasFlag(PluginState.Installed) == y.State.HasFlag(PluginState.Installed))
                    {
                        return string.Compare(x.Title, y.Title, StringComparison.CurrentCultureIgnoreCase);
                    }

                    return x.State.HasFlag(PluginState.Installed) ? -1 : 1;
                }

                return -1;
            }

            return y != null ? 1 : 0;
        }
    }

    internal class PluginSortMostRecent : IComparer, IComparer<IPluginVM>
    {
        int IComparer.Compare(object x, object y)
        {
            return this.Compare(x as IPluginVM, y as IPluginVM);
        }

        public int Compare(IPluginVM x, IPluginVM y)
        {
            if (x != null)
            {
                if (y != null)
                {
                    if (x.LatestVersionDate == y.LatestVersionDate)
                    {
                        return string.Compare(x.Title, y.Title, StringComparison.CurrentCultureIgnoreCase);
                    }

                    return DateTime.Compare(x.LatestVersionDate, y.LatestVersionDate);
                }

                return -1;
            }

            return y != null ? 1 : 0;
        }
    }

    internal class PluginSortName : IComparer, IComparer<IPluginVM>
    {
        int IComparer.Compare(object x, object y)
        {
            return this.Compare(x as IPluginVM, y as IPluginVM);
        }

        public int Compare(IPluginVM x, IPluginVM y)
        {
            return string.Compare(x?.Title ?? string.Empty, y?.Title ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
        }
    }

}
