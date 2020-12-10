using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace PodcastMusicSwitcher
{
    public class PlaylistReader
    {
        public string FileName { get; set; }

        public Collection<string> GetItems(string fileName)
        {
            var directoryName = Path.GetDirectoryName(fileName);
            if (directoryName == null)
            {
                return new Collection<string>();
            }

            XElement document = XElement.Load(fileName);
            var sources = new Collection<string>();
            foreach (var mediaElement in document.Descendants("media"))
            {
                if (mediaElement.Attribute("src") != null)
                {
                    sources.Add(Path.GetFullPath(Path.Combine(directoryName, mediaElement.Attribute("src").Value)));
                }
            }

            return sources;
        }
    }
}