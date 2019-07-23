using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    internal struct NuGetSearchResult
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        [DataMember(Name = "@type")]
        public string type;

        public string registration;
        public string id;
        public string version;
        public string description;
        public string summary;
        public string title;
        public string iconUrl;
        public string projectUrl;
        public string[] tags;
        public string[] authors;
        public int totalDownloads;
        public bool verified;
        public NuGetSearchResultVersion[] versions;
    }
}
