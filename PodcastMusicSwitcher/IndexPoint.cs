using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class IndexPoint
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "startPoint")]
        public string StartPoint { get; set; }

        public string Display => $"{StartPoint} - {Title}";
    }
}