using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class PrePlay
    {
        [DataMember(Name = "indexPoints")]
        public Collection<IndexPoint> IndexPoints { get; set; }
    }
}