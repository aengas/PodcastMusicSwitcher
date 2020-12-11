using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class Titles
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "subtitle")]
        public string SubTitle { get; set; }
    }
}