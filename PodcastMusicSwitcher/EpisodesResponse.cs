using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class EpisodesResponse
    {
        [DataMember(Name = "seriesType")]
        public string SeriesType { get; set; }

        [DataMember(Name = "_embedded")]
        public Embedded Embedded { get; set; }
    }
}