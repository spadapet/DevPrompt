using System;
using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    internal struct NuGetPackageVersionInfo
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        [DataMember(Name = "@type")]
        public string[] type;

        public string catalogEntry;
        public bool listed;
        public string packageContent;
        public DateTime published;
        public string registration;
    }
}
