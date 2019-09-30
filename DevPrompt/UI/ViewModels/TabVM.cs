using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.UI.ViewModels
{
    /// <summary>
    /// View model to wrap Api.ITab model
    /// </summary>
    public class TabVM : PropertyNotifier, ITabVM
    {
        public string Title => this.Tab?.Title ?? string.Empty;
        public UIElement ViewElement => this.Tab?.ViewElement;
        public bool CreatedTab => this.tab != null;
        public IEnumerable<FrameworkElement> ContextMenuItems => this.tab?.ContextMenuItems ?? Enumerable.Empty<FrameworkElement>();

        private Api.IWindow window;
        private Api.ITab tab;
        private Api.ITabWorkspace workspace;
        private Api.ITabSnapshot snapshot;
        private Api.ActiveState activeState;
        private bool restoring;

        private TabVM(Api.IWindow window, Api.ITabWorkspace workspace)
        {
            this.window = window;
            this.workspace = workspace;
        }

        public TabVM(Api.IWindow window, Api.ITabWorkspace workspace, Api.ITab tab)
            : this(window, workspace)
        {
            this.InitTab(tab);
        }

        public TabVM(Api.IWindow window, Api.ITabWorkspace workspace, Api.ITabSnapshot snapshot)
            : this(window, workspace)
        {
            this.snapshot = snapshot;
        }

        public void Dispose()
        {
            if (this.tab != null)
            {
                this.tab.PropertyChanged -= this.OnTabPropertyChanged;
                (this.tab as IDisposable)?.Dispose();
            }
        }

        private void InitTab(Api.ITab tab)
        {
            Debug.Assert(this.tab == null || this.tab == tab);
            if (this.tab == null)
            {
                this.tab = tab;

                if (this.tab != null)
                {
                    this.tab.PropertyChanged += this.OnTabPropertyChanged;
                }

                this.OnPropertyChanged(nameof(this.Tab));
                this.OnPropertyChanged(nameof(this.Name));
                this.OnPropertyChanged(nameof(this.Tooltip));
                this.OnPropertyChanged(nameof(this.ContextMenuItems));
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITab.Name))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITab.Title))
            {
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITab.Tooltip))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITab.ViewElement))
            {
                this.OnPropertyChanged(nameof(this.ViewElement));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(Api.ITab.ContextMenuItems))
            {
                this.OnPropertyChanged(nameof(this.ContextMenuItems));
            }
        }

        public Guid Id
        {
            get
            {
                if (this.tab != null)
                {
                    return this.tab.Id;
                }

                if (this.snapshot != null)
                {
                    return this.snapshot.Id;
                }

                return Guid.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (this.tab != null)
                {
                    return this.tab.Name;
                }

                if (this.snapshot != null)
                {
                    return this.snapshot.Name;
                }

                return string.Empty;
            }
        }

        public string Tooltip
        {
            get
            {
                if (this.tab != null)
                {
                    return this.tab.Tooltip;
                }

                if (this.snapshot != null)
                {
                    return this.snapshot.Tooltip;
                }

                return string.Empty;
            }
        }

        public Api.ITabSnapshot Snapshot
        {
            get
            {
                return this.tab?.Snapshot ?? this.snapshot;
            }
        }

        public Api.ITab Tab
        {
            get
            {
                if (this.tab == null && this.snapshot != null)
                {
                    Api.ITabSnapshot snapshot = this.snapshot;
                    this.snapshot = null;

                    try
                    {
                        this.restoring = true;
                        this.InitTab(snapshot.Restore(this.window, this.workspace));
                    }
                    finally
                    {
                        this.restoring = false;
                    }
                }

                return this.tab;
            }
        }

        public Api.ActiveState ActiveState
        {
            get
            {
                return this.activeState;
            }

            set
            {
                if (this.SetPropertyValue(ref this.activeState, value))
                {
                    if (this.activeState == Api.ActiveState.Hidden)
                    {
                        this.Tab?.OnHiding();
                    }
                    else
                    {
                        this.Tab?.OnShowing();
                    }
                }
            }
        }

        public ICommand ActivateCommand => new DelegateCommand(() =>
        {
            this.workspace.ActiveTab = this;
        });

        public ICommand CloseCommand => new DelegateCommand(() =>
        {
            if (this.tab == null || this.tab.OnClosing())
            {
                this.workspace.RemoveTab(this);
            }
        });

        public ICommand CloseAllButThisCommand => new DelegateCommand(() =>
        {
            foreach (ITabVM tab in this.workspace.Tabs.OfType<ITabVM>().ToArray())
            {
                if (tab != this && tab.CloseCommand != null && tab.CloseCommand.CanExecute(null))
                {
                    tab.CloseCommand.Execute(null);
                }
            }
        });

        public void Focus()
        {
            this.Tab?.Focus();
        }

        public bool TakeRestoredTab(Api.ITab tab)
        {
            // There can only be one tab restoring at a time
            if (this.restoring)
            {
                this.InitTab(tab);
                return true;
            }

            return false;
        }
    }
}
