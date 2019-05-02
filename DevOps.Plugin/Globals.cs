using DevPrompt.Plugins;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DevOps
{
    /// <summary>
    /// Created once when the app starts up
    /// </summary>
    internal class Globals : IDisposable
    {
        public static Globals Instance { get; private set; }
        public IApp App { get; }
        private readonly ConcurrentDictionary<string, VssConnection> vssConnections;
        private CancellationTokenSource cancellationTokenSource;

        public Globals(IApp app)
        {
            Debug.Assert(Globals.Instance == null);
            if (Globals.Instance == null)
            {
                Globals.Instance = this;
            }

            this.App = app;
            this.vssConnections = new ConcurrentDictionary<string, VssConnection>();
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;

            foreach (VssConnection connection in this.vssConnections.Values)
            {
                connection.Dispose();
            }

            this.vssConnections.Clear();

            Debug.Assert(Globals.Instance == this);
            if (Globals.Instance == this)
            {
                Globals.Instance = null;
            }
        }

        public async Task<VssConnection> GetVssConnection(string organizationName, string personalAccessToken)
        {
            if (!this.vssConnections.TryGetValue(organizationName, out VssConnection connection))
            {
                Uri uri = await VssConnectionHelper.GetOrganizationUrlAsync(organizationName, this.cancellationTokenSource.Token);
                VssCredentials credentials = new VssBasicCredential(string.Empty, personalAccessToken);
                connection = new VssConnection(uri, credentials);
                this.vssConnections.TryAdd(organizationName, connection);
            }

            return connection;
        }
    }
}
