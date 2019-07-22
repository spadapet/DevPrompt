using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    internal struct NuGetSearchResultVersion
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        public string version;
        public int downloads;
    }
}
