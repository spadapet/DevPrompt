using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    [DataContract]
    internal struct NuGetSearchResult
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        [DataMember(Name = "@type")]
        public string type;

        [DataMember] public string registration;
        [DataMember] public string id;
        [DataMember] public string version;
        [DataMember] public string description;
        [DataMember] public string summary;
        [DataMember] public string title;
        [DataMember] public string iconUrl;
        [DataMember] public string projectUrl;
        [DataMember] public string[] tags;
        [DataMember] public string[] authors;
        [DataMember] public int totalDownloads;
        [DataMember] public bool verified;
        [DataMember] public NuGetSearchResultVersion[] versions;
    }
}
