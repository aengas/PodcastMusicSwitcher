using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class Availability
    {
        [DataMember(Name = "onDemand")]
        public OnDemand OnDemand { get; set; }
    }
}