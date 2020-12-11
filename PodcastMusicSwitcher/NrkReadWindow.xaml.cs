using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Xml;

namespace PodcastMusicSwitcher
{
    /// <summary>
    /// Interaction logic for NrkReadWindow.xaml
    /// </summary>
    public partial class NrkReadWindow : Window
    {
        private readonly MainWindow m_mainWindow;

        public NrkReadWindow(MainWindow mainWindow)
        {
            m_mainWindow = mainWindow;
            InitializeComponent();
            serieTextBox.Text = "radioresepsjonen";
            seasonTextBox.Text = "201909";
            GetEpisodesButton_OnClick(null, null);
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
            if (selectedEpisode == null)
            {
                return;
            }
            string metadataRequest = $"https://psapi.nrk.no/playback/metadata/program/{selectedEpisode.EpisodeId}";
            var metadataRequester = new Requester<MetadataResponse>();
            MetadataResponse metadataResponse = metadataRequester.Request(metadataRequest);

            string playlistRequest = $"http://psapi-granitt-prod-ne.cloudapp.net/programs/{selectedEpisode.EpisodeId}/playlist";
            var playlistRequester = new Requester<Collection<Song>>();
            Collection<Song> playlistResponse = playlistRequester.Request(playlistRequest);

            int totalCount = metadataResponse.PrePlay.IndexPoints.Count + playlistResponse.Count;

            DateTime startTime = DateTime.Parse(metadataResponse.Availability.OnDemand.From);

            var timePointItemsCollection = new Collection<TimePointItem>();

            foreach (IndexPoint indexPoint in metadataResponse.PrePlay.IndexPoints)
            {
                timePointItemsCollection.Add(new TimePointItem
                {
                    StartTime = startTime.Add(ConvertStartPointToTimeSpan(indexPoint)),
                    Description = indexPoint.Title,
                    Type = "snakk"
                });
            }

            foreach (Song song in playlistResponse)
            {
                timePointItemsCollection.Add(new TimePointItem
                {
                    StartTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(song.StartTime.Substring(6, song.StartTime.Length - 13))).LocalDateTime,
                    Description = $"{song.Title} - {song.Description}",
                    Duration = XmlConvert.ToTimeSpan(song.Duration),
                    Type = "sang"
                });
            
            }

            TimeSpan fromBeginning = new TimeSpan(0, 0, 0, 0);
            TimeSpan lastFromBeginningWithoutSong = new TimeSpan(0, 0, 0, 0);
            DateTime lastSnakk = DateTime.MinValue;
            Collection<TimePointItem> songsBetween = new Collection<TimePointItem>();
            int snakkCount = 0;
            foreach (TimePointItem tpi in timePointItemsCollection.OrderBy(t => t.StartTime))
            {
                if (tpi.Type == "snakk" && snakkCount == 0)
                {
                    tpi.FromBeginningWithoutSong = fromBeginning;
                    lastFromBeginningWithoutSong = tpi.FromBeginningWithoutSong;
                    lastSnakk = tpi.StartTime;
                    snakkCount++;
                    songsBetween.Clear();
                }

                if (tpi.Type == "snakk" && snakkCount > 0)
                {
                    TimeSpan timespanBetween = new TimeSpan(0, 0, 0, 0);
                    foreach (TimePointItem songBetween in songsBetween)
                    {
                        timespanBetween = timespanBetween.Add(songBetween.Duration);
                    }

                    tpi.FromBeginningWithoutSong = lastFromBeginningWithoutSong.Add(tpi.StartTime - lastSnakk - timespanBetween);
                    lastFromBeginningWithoutSong = tpi.FromBeginningWithoutSong;
                    lastSnakk = tpi.StartTime;
                    snakkCount++;
                    songsBetween.Clear();
                    talks.Add(tpi);
                }

                if (tpi.Type == "sang")
                {
                    songsBetween.Add(tpi);
                }

                EpisodeDetailsListBoxs.Items.Add(tpi);
            }
        }

        private static TimeSpan ConvertStartPointToTimeSpan(IndexPoint indexPoint)
        {
            TimeSpan parsed = XmlConvert.ToTimeSpan(indexPoint.StartPoint);

            return parsed;
        }

        private Collection<TimePointItem> talks = new Collection<TimePointItem>();

        private void TransferTimesButton_OnClick(object sender, RoutedEventArgs e)
        {
            m_mainWindow.ChangeTimesComboBox.Items.Clear(); 
            foreach (TimePointItem talk in talks)
            {
                
                if (talk.FromBeginningWithoutSong.TotalSeconds > 0)
                {
                    m_mainWindow.ChangeTimesComboBox.Items.Add(new TimeSpan(0, talk.FromBeginningWithoutSong.Hours, talk.FromBeginningWithoutSong.Minutes, talk.FromBeginningWithoutSong.Seconds));
                }
            }

            m_mainWindow.ChangeTimesComboBox.SelectedIndex = 0;
            
            this.Close();   
        }
    }

    public class TimePointItem
    {
        public DateTime StartTime { get; set; }

        public string Description { get; set; }

        public TimeSpan Duration { get; set; }

        public string Type { get; set; }

        public TimeSpan FromBeginningWithoutSong { get; set; }

        public string Display => $"{StartTime} - {Description} - {FromBeginningWithoutSong}";
    }
}
