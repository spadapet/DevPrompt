using DevPrompt.Api;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps
{
    /// <summary>
    /// Stores global data for this plugin, only one will ever be created at a time.
    /// VSS connections are cached here so that they can be shared among views.
    /// </summary>
    [Export(typeof(IAppListener))]
    internal class Globals : IAppListener, IDisposable
    {
        public static Globals Instance { get; private set; }
        private readonly Dictionary<string, VssConnection> vssConnections;

        public Globals()
        {
            Globals.Instance = this;

            this.vssConnections = new Dictionary<string, VssConnection>();
        }

        public void Dispose()
        {
            foreach (VssConnection connection in this.vssConnections.Values)
            {
                connection.Dispose();
            }

            this.vssConnections.Clear();

            Globals.Instance = null;
        }

        public async Task<VssConnection> GetVssConnection(string organizationName, string personalAccessToken, CancellationToken cancellationToken)
        {
            if (!this.vssConnections.TryGetValue(organizationName, out VssConnection connection))
            {
                Uri uri = await VssConnectionHelper.GetOrganizationUrlAsync(organizationName, cancellationToken);

                // Check again in case a connection was added during the async call
                if (!this.vssConnections.TryGetValue(organizationName, out connection))
                {
                    VssCredentials credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                    connection = new VssConnection(uri, credentials);

                    this.vssConnections[organizationName] = connection;
                }
            }

            return connection;
        }

        void IAppListener.OnStartup(IApp app)
        {
        }

        void IAppListener.OnOpened(IApp app, IWindow window)
        {
        }

        void IAppListener.OnClosing(IApp app, IWindow window)
        {
        }

        void IAppListener.OnExit(IApp app)
        {
        }
    }
}
