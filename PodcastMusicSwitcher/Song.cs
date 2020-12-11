using System;
using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class Song
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "programId")]
        public string ProgramId { get; set; }

        [DataMember(Name = "channelId")]
        public string ChannelId { get; set; }

        [DataMember(Name = "startTime")]
        public string StartTime { get; set; }

        [DataMember(Name = "duration")]
        public string Duration { get; set; }

        public string Display => $"{DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(StartTime.Substring(6, StartTime.Length - 13))).LocalDateTime} - {Duration} - {Title} - {Description}";
    }
}