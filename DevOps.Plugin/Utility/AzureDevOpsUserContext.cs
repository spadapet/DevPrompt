using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Client;
using System.Collections.Generic;

namespace DevOps.Utility
{
    public class AzureDevOpsUserContext
    {
        public AuthenticationResult AuthenticationResult { get; set; }
        public VssAadCredential VssAadCredential { get; set; }
        public IEnumerable<Account> Accounts { get; set; }
    }
}
