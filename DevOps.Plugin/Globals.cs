using DevPrompt.Api;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps
{
    /// <summary>
    /// The intention of this Globals class is to cache VSS connections
    /// globally so that each view doesn't have to authenticate.
    /// The implementation is not here yet, but that's the intent.
    /// </summary>
    [Export(typeof(IAppListener))]
    internal class Globals : IAppListener, IDisposable
    {
        public static Globals Instance { get; private set; }
        public IApp App { get; }
        private readonly ConcurrentDictionary<string, VssConnection> vssConnections;
        private bool disposed;

        [ImportingConstructor]
        public Globals(IApp app)
        {
            Globals.Instance = this;

            this.App = app;
            this.vssConnections = new ConcurrentDictionary<string, VssConnection>();
        }

        public void Dispose()
        {
            Debug.Assert(!this.disposed);

            if (!this.disposed)
            {
                this.disposed = true;

                foreach (VssConnection connection in this.vssConnections.Values)
                {
                    connection.Dispose();
                }

                this.vssConnections.Clear();
            }

            Globals.Instance = null;
        }

        public async Task<VssConnection> GetVssConnection(string organizationName, string personalAccessToken, CancellationToken cancellationToken)
        {
            if (!this.vssConnections.TryGetValue(organizationName, out VssConnection connection))
            {
                Uri uri = await VssConnectionHelper.GetOrganizationUrlAsync(organizationName, cancellationToken);
                VssCredentials credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                connection = new VssConnection(uri, credentials);
                this.vssConnections.TryAdd(organizationName, connection);
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
