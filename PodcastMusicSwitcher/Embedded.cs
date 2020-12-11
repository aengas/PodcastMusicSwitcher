using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PodcastMusicSwitcher
{
    [DataContract]
    public class Embedded
    {
        [DataMember(Name = "episodes")]
        public Collection<Episode> Episodes { get; set; }
    }
}