using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class MetadataResponse
    {
        [DataMember(Name = "availability")]
        public Availability Availability { get; set; }

        [DataMember(Name = "preplay")]
        public PrePlay PrePlay { get; set; }
    }
}