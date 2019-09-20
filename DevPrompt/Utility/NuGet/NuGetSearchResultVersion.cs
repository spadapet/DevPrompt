using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    [DataContract]
    internal struct NuGetSearchResultVersion
    {
        [DataMember(Name = "@id")]
        public string idUrl;

        [DataMember] public string version;
        [DataMember] public int downloads;
    }
}
