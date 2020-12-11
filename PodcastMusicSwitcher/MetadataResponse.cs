using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class MetadataResponse
    {
        [DataMember(Name = "preplay")]
        public PrePlay PrePlay { get; set; }
    }
}