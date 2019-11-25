using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Settings;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DevPrompt.UI.Settings
{
    internal partial class ColorsSettingsControl : UserControl
    {
        public SettingsDialogVM ViewModel { get; }
        public DelegateCommand MoveUpCommand { get; }
        public DelegateCommand MoveDownCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand ResetCommand { get; }
        private int[] previousCustomColors;

        public ColorsSettingsControl(SettingsDialogVM viewModel)
        {
            this.ViewModel = viewModel;
            this.ViewModel.Settings.ObservableTabThemes.CollectionChanged += this.OnSettingsChanged;

            this.MoveUpCommand = CommandUtility.CreateMoveUpCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTabThemes);
            this.MoveDownCommand = CommandUtility.CreateMoveDownCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTabThemes);
            this.DeleteCommand = CommandUtility.CreateDeleteCommand(() => this.dataGrid, this.ViewModel.Settings.ObservableTabThemes);
            this.ResetCommand = CommandUtility.CreateResetCommand(s => s.ObservableTabThemes, this.ViewModel.Settings, AppSettings.DefaultSettingsFilter.TabThemeKeys);

            this.InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            CommandUtility.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnSettingsChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            CommandUtility.UpdateCommands(this.Dispatcher, this.MoveUpCommand, this.MoveDownCommand, this.DeleteCommand);
        }

        private void OnDataGridBeginningEdit(object sender, DataGridBeginningEditEventArgs args)
        {
            if (args.Column == this.colorColumn && args.Row.DataContext is TabTheme tabTheme)
            {
                args.Cancel = true;

                Action action = () =>
                {
                    ColorDialog dialog = new ColorDialog(tabTheme.ThemeKeyColor)
                    {
                        CustomColors = this.previousCustomColors,
                    };

                    if (dialog.ShowDialog(this.ViewModel.Dialog))
                    {
                        this.previousCustomColors = dialog.CustomColors;
                        tabTheme.ThemeKeyColor = dialog.WpfColor;
                    }
                };

                this.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
        }

        private class WindowInterop : System.Windows.Forms.IWin32Window, IWin32Window
        {
            public IntPtr Handle { get; }

            public WindowInterop(Window window)
            {
                this.Handle = new WindowInteropHelper(window).Handle;
            }
        }

        private class ColorDialog : System.Windows.Forms.ColorDialog
        {
            public ColorDialog(Color color)
            {
                this.AllowFullOpen = true;
                this.AnyColor = true;
                this.FullOpen = true;
                this.ShowHelp = false;
                this.SolidColorOnly = true;
                this.WpfColor = color;
            }

            public Color WpfColor
            {
                get => System.Windows.Media.Color.FromRgb(base.Color.R, base.Color.G, base.Color.B);
                set => base.Color = System.Drawing.Color.FromArgb(value.R, value.G, value.B);
            }

            public bool ShowDialog(Window window)
            {
                return this.ShowDialog(new WindowInterop(window)) == System.Windows.Forms.DialogResult.OK;
            }

            protected override IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam)
            {
                // Don't let the base class center the dialog on the screen, but still have to do the other work that it does for WM_INITDIALOG
                if (msg == ColorDialog.WM_INITDIALOG)
                {
                    if (typeof(System.Windows.Forms.CommonDialog).GetField("defaultControlHwnd", BindingFlags.Instance | BindingFlags.NonPublic) is FieldInfo defaultControlHwndField)
                    {
                        defaultControlHwndField.SetValue(this, wparam);
                    }

                    ColorDialog.SetFocus(wparam);
                    return IntPtr.Zero;
                }

                return base.HookProc(hwnd, msg, wparam, lparam);
            }

            [DllImport("User32")]
            private static extern IntPtr SetFocus(IntPtr hwnd);
            private const int WM_INITDIALOG = 0x0110;
        }
    }
}
