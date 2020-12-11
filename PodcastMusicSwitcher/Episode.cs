using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class Episode
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "episodeId")]
        public string EpisodeId { get; set; }

        [DataMember(Name = "titles")]
        public Titles Titles { get; set; }

        [DataMember(Name = "duration")]
        public string Duration { get; set; }

        [DataMember(Name = "date")]
        public string Date { get; set; }

        [DataMember(Name = "durationInSeconds")]
        public int DurationInSeconds { get; set; }
    }
}