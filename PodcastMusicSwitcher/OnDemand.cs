using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class OnDemand
    {
        [DataMember(Name = "from")]
        public string From { get; set; }

        [DataMember(Name = "to")]
        public string To { get; set; }

        [DataMember(Name = "hasRightsNow")]
        public string HasRightsNow { get; set; }
    }
}