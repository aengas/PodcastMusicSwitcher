using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PodcastMusicSwitcher
{
    /// <summary>
    /// Interaction logic for NrkReadWindow.xaml
    /// </summary>
    public partial class NrkReadWindow : Window
    {
        public NrkReadWindow()
        {
            InitializeComponent();
            serieTextBox.Text = "radioresepsjonen";
            seasonTextBox.Text = "201909";
        }

        private void GetEpisodesButton_OnClick(object sender, RoutedEventArgs e)
        {
            EpisodesListBox.Items.Clear();
            string episodesRequest = $"https://psapi.nrk.no/radio/catalog/series/{serieTextBox.Text}/seasons/{seasonTextBox.Text}/episodes?pageSize=30&sort=desc";
            var episodesRequester = new Requester<EpisodesResponse>();
            EpisodesResponse repositoryResponse = episodesRequester.Request(episodesRequest);
            foreach (Episode episode in repositoryResponse.Embedded.Episodes)
            {
                EpisodesListBox.Items.Add(episode);
            }
        }

        private void EpisodesListBox_OnSelected(object sender, RoutedEventArgs e)
        {
            EpisodeDetailsListBoxs.Items.Clear();
            var selectedEpisode = (Episode)EpisodesListBox.SelectedItem;
            string metadataRequest = $"https://psapi.nrk.no/playback/metadata/program/{selectedEpisode.EpisodeId}";
            var metadataRequester = new Requester<MetadataResponse>();
            MetadataResponse metadataResponse = metadataRequester.Request(metadataRequest);
            foreach (IndexPoint indexPoint in metadataResponse.PrePlay.IndexPoints)
            {
                EpisodeDetailsListBoxs.Items.Add(indexPoint);
            }

            string playlistRequest = $"http://psapi-granitt-prod-ne.cloudapp.net/programs/{selectedEpisode.EpisodeId}/playlist";
            var playlistRequester = new Requester<Collection<Song>>();
            Collection<Song> playlistResponse = playlistRequester.Request(playlistRequest);
            foreach (Song song in playlistResponse)
            {
                EpisodeDetailsListBoxs.Items.Add(song);
            }
        }
    }
}
