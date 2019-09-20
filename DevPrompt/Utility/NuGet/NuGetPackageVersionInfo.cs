using System;
using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    [DataContract]
    internal struct NuGetPackageVersionInfo
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        [DataMember(Name = "@type")]
        public string[] type;

        [DataMember] public string catalogEntry;
        [DataMember] public bool listed;
        [DataMember] public string packageContent;
        [DataMember] public DateTime published;
        [DataMember] public string registration;
    }
}
