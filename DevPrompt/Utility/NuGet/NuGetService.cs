using System.Runtime.Serialization;

namespace DevPrompt.Utility.NuGet
{
    internal struct NuGetService
    {
        [DataMember(Name = "@type")]
        public string type;

        [DataMember(Name = "@id")]
        public string idUrl;
    }
}
