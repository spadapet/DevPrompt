using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// View model to wrap ITab model
    /// </summary>
    public class TabVM : PropertyNotifier, ITabVM
    {
        public string Title => this.Tab?.Title ?? string.Empty;
        public UIElement ViewElement => this.Tab?.ViewElement;
        public bool CreatedTab => this.tab != null;
        public ICommand CloneCommand => this.Tab?.CloneCommand;
        public ICommand DetachCommand => this.Tab?.DetachCommand;
        public ICommand DefaultsCommand => this.Tab?.DefaultsCommand;
        public ICommand PropertiesCommand => this.Tab?.PropertiesCommand;
        public ICommand SetTabNameCommand => this.Tab?.SetTabNameCommand;

        private IWindow window;
        private ITab tab;
        private ITabWorkspace workspace;
        private ITabSnapshot snapshot;
        private ActiveState activeState;
        private bool restoring;

        private TabVM(IWindow window, ITabWorkspace workspace)
        {
            this.window = window;
            this.workspace = workspace;
        }

        public TabVM(IWindow window, ITabWorkspace workspace, ITab tab)
            : this(window, workspace)
        {
            this.InitTab(tab);
        }

        public TabVM(IWindow window, ITabWorkspace workspace, ITabSnapshot snapshot)
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

        private void InitTab(ITab tab)
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
            }
        }

        private void OnTabPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITab.Name))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITab.Title))
            {
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITab.Tooltip))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(ITab.ViewElement))
            {
                this.OnPropertyChanged(nameof(this.ViewElement));
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

        public ITabSnapshot Snapshot
        {
            get
            {
                return this.tab?.Snapshot ?? this.snapshot;
            }
        }

        public ITab Tab
        {
            get
            {
                if (this.tab == null && this.snapshot != null)
                {
                    ITabSnapshot snapshot = this.snapshot;
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

        public ActiveState ActiveState
        {
            get
            {
                return this.activeState;
            }

            set
            {
                if (this.SetPropertyValue(ref this.activeState, value))
                {
                    if (this.activeState == ActiveState.Hidden)
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
            foreach (ITabVM tab in this.workspace.Tabs.ToArray())
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

        public bool TakeRestoredTab(ITab tab)
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
