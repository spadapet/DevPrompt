using DevPrompt.UI.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DevPrompt.Utility
{
    internal static class BrowserUtility
    {
        private const string DefaultId = "";
        private const string EdgeId = "microsoft-edge:";
        private const string NewEdgePrefix = "Microsoft Edge";
        private const string EdgePackageKey = @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData\Microsoft.MicrosoftEdge_8wekyb3d8bbwe\Schemas";
        private const string EdgePackageValue = "PackageFullName";
        private const string BrowsersKey = @"Software\Clients\StartMenuInternet";
        private const string BrowsersKey32 = @"Software\WOW6432Node\Clients\StartMenuInternet";
        private const string CommandKey = @"shell\open\command";

        internal class BrowserInfo : IEquatable<BrowserInfo>
        {
            public string Id { get; }
            public string Name { get; }

            public BrowserInfo(string id, string name)
            {
                this.Id = id ?? string.Empty;
                this.Name = !string.IsNullOrEmpty(name) ? name : (!string.IsNullOrEmpty(id) ? id : Resources.Browsers_DefaultBrowserName);
            }

            public override string ToString()
            {
                return this.Name;
            }

            public override bool Equals(object obj)
            {
                return obj is BrowserInfo other && this.Equals(other);
            }

            public bool Equals(BrowserInfo other)
            {
                return this.Id == other.Id;
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode();
            }
        }

        public static IEnumerable<BrowserInfo> GetInstalledBrowsers()
        {
            List<BrowserInfo> browsers = new List<BrowserInfo>();
            HashSet<string> found = new HashSet<string>();

            browsers.Add(new BrowserInfo(string.Empty, string.Empty));
            found.Add(string.Empty);

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(BrowserUtility.BrowsersKey))
                {
                    foreach (string id in key?.GetSubKeyNames() ?? Enumerable.Empty<string>())
                    {
                        if (found.Add(id))
                        {
                            using (RegistryKey subKey = key.OpenSubKey(id))
                            {
                                if (subKey.GetValue(null) is string name)
                                {
                                    browsers.Add(new BrowserInfo(id, name));
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // oh well
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(BrowserUtility.BrowsersKey32))
                {
                    foreach (string id in key?.GetSubKeyNames() ?? Enumerable.Empty<string>())
                    {
                        if (found.Add(id))
                        {
                            using (RegistryKey subKey = key.OpenSubKey(id))
                            {
                                if (subKey.GetValue(null) is string name)
                                {
                                    browsers.Add(new BrowserInfo(id, name));
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // oh well
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(BrowserUtility.EdgePackageKey))
                {
                    if (key != null & key.GetValue(BrowserUtility.EdgePackageValue) != null)
                    {
                        browsers.Add(new BrowserInfo(BrowserUtility.EdgeId,
                            found.Any(s => s.StartsWith(BrowserUtility.NewEdgePrefix))
                                ? Resources.Browsers_EdgeLegacyName
                                : Resources.Browsers_EdgeName));
                    }
                }
            }
            catch
            {
                // oh well
            }

            return browsers;
        }

        public static void Browse(string browserId, string url, MainWindowVM mainWindowVM)
        {
            switch (browserId ?? string.Empty)
            {
                case BrowserUtility.DefaultId:
                    mainWindowVM.RunExternalProcess(url);
                    break;

                case BrowserUtility.EdgeId:
                    mainWindowVM.RunExternalProcess($"{BrowserUtility.EdgeId}{url}");
                    break;

                default:
                    bool success = false;
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey($@"{BrowserUtility.BrowsersKey}\{browserId}\{BrowserUtility.CommandKey}") ??
                            Registry.LocalMachine.OpenSubKey($@"{BrowserUtility.BrowsersKey32}\{browserId}\{BrowserUtility.CommandKey}"))
                        {
                            if (key?.GetValue(null) is string command)
                            {
                                command = command.Trim('\"');
                                if (File.Exists(command))
                                {
                                    mainWindowVM.RunExternalProcess(command, url);
                                    success = true;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Use default instead
                    }

                    if (!success)
                    {
                        mainWindowVM.RunExternalProcess(url);
                    }
                    break;
            }
        }
    }
}
