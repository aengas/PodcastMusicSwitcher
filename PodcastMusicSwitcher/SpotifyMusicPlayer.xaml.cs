using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace PodcastMusicSwitcher
{
    public partial class SpotifyMusicPlayer
    {
        private bool m_mediaPlayerIsPlaying;
        private bool m_userIsDraggingSlider;

        private int m_playlistIndex;
        private Collection<string> m_playlist;

        private readonly Collection<int> m_playedIndices;
        private int m_currentPlayingIndexOfPlayedIndices;

        public string DefaultPath { get; set; }
        public string FileFilter { get; set; }

        public string SpecifiedFile { get; set; }
        public string FilePlaying
        {
            get => mePlayer.Source.LocalPath;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    mePlayer.Source = new Uri(value);
                    m_playlistIndex = m_playlist.IndexOf(value);
                    m_playedIndices.Add(m_playlistIndex);
                    LoadSongInfo(value);
                }
            }
        }
        public TimeSpan Position => mePlayer.Position;

        private TimeSpan m_desiredPosition;

        public void SetPosition(TimeSpan position)
        {
            m_desiredPosition = position;
            mePlayer.Pause();
        }

        public SpotifyMusicPlayer()
        {
            InitializeComponent();
            m_playedIndices = new Collection<int>();
            mePlayer.MediaOpened += MePlayer_MediaOpened;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void MePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            mePlayer.Position = m_desiredPosition;
        }

        public event EventHandler<EventArgs> MediaEnded;
        public event EventHandler<EventArgs> MediaLoaded;

        private void TimerTick(object sender, EventArgs e)
        {
            if ((mePlayer.Source == null) || (!mePlayer.NaturalDuration.HasTimeSpan) || (m_userIsDraggingSlider))
            {
                return;
            }

            sliProgress.Minimum = 0;
            sliProgress.Maximum = mePlayer.NaturalDuration.TimeSpan.TotalSeconds;
            lblDuration.Text = mePlayer.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            sliProgress.Value = mePlayer.Position.TotalSeconds;
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Open();
        }

        public void Open()
        {
            if (!File.Exists(DefaultPath))
            {
                DefaultPath = @"C:\";
            }
            var openFileDialog = new OpenFileDialog { Filter = FileFilter, InitialDirectory = DefaultPath };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            LoadFile(openFileDialog.FileName);
        }

        public void LoadFile(string fileName)
        {
            SpecifiedFile = fileName;
            mePlayer.Source = new Uri(SpecifiedFile);

            MusicFileName.Text = SpecifiedFile;
            string extension = Path.GetExtension(SpecifiedFile);

            m_playlist = new Collection<string>();
            switch (extension.ToLower())
            {
                case ".mp3":
                    m_playlist.Add(SpecifiedFile);
                    m_desiredPosition = new TimeSpan(0, 0, 0);
                    break;
                case ".wpl":
                    var playlistReader = new PlaylistReader();
                    m_playlist = playlistReader.GetItems(SpecifiedFile);
                    m_playlistIndex = 0;
                    if (m_playlist.Count > 0)
                    {
                        mePlayer.Source = new Uri(m_playlist[m_playlistIndex]);
                    }
                    else
                    {
                        MessageBox.Show("Spillelisten er tom");
                    }
                    break;
            }

            LoadSongInfo(m_playlist[m_playlistIndex]);
        }

        private void LoadSongInfo(string fileName)
        {
            IndexLabel.Content = $"({m_playlistIndex + 1}/{m_playlist.Count})";

            if (string.Compare(Path.GetExtension(fileName), ".mp3", true, CultureInfo.InvariantCulture) != 0)
            {
                SongTitleLabel.Content = Path.GetFileName(fileName);
                PerformerLabel.Content = string.Empty;
                AlbumArtistLabel.Content = string.Empty;
                AlbumLabel.Content = string.Empty;
                TrackLabel.Content = string.Empty;
                YearLabel.Content = string.Empty;
                ComposerLabel.Content = string.Empty;
                return;
            }

            if (!File.Exists(fileName))
            {
                MessageBox.Show(fileName + " does not exist");
                return;
            }
            TagLib.File f = TagLib.File.Create(fileName);

            SongTitleLabel.Content = f.Tag.Title;
            PerformerLabel.Content = f.Tag.JoinedPerformers;
            AlbumArtistLabel.Content = f.Tag.JoinedAlbumArtists;
            AlbumLabel.Content = f.Tag.Album;
            TrackLabel.Content = f.Tag.Track;
            YearLabel.Content = f.Tag.Year;
            ComposerLabel.Content = f.Tag.JoinedComposers;

            lblDuration.Text = f.Properties.Duration.ToString(@"hh\:mm\:ss");
        }

        public void Next()
        {
            m_playlistIndex = GetNextPlaylistIndex();
            m_playedIndices.Add(m_playlistIndex);
            m_currentPlayingIndexOfPlayedIndices++;
            LoadSong();
        }

        private void LoadSong()
        {
            string nextFileName = m_playlist[m_playlistIndex];
            mePlayer.Source = new Uri(nextFileName);
            LoadSongInfo(nextFileName);
            mePlayer.Pause();
            m_desiredPosition = new TimeSpan(0);
        }

        private int GetNextPlaylistIndex()
        {
            if (ShuffleCheckBox.IsChecked == true)
            {
                if (m_currentPlayingIndexOfPlayedIndices == m_playedIndices.Count - 1)
                {
                    return new Random().Next(0, m_playlist.Count);
                }
                
                return m_playedIndices[++m_currentPlayingIndexOfPlayedIndices];
            }

            return m_playlistIndex < m_playlist.Count - 1 ? ++m_playlistIndex : 0;
        }

        private void Previous()
        {
            if (m_currentPlayingIndexOfPlayedIndices > 1)
            {
                m_currentPlayingIndexOfPlayedIndices--;
                m_playlistIndex = m_playedIndices[m_currentPlayingIndexOfPlayedIndices];
            }
            else if (ShuffleCheckBox.IsChecked == false)
            {
                m_playlistIndex = m_playlistIndex > 0 ? --m_playlistIndex : 0;
            }
            LoadSong();
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Play();
        }

        public void Play()
        {
            mePlayer.Play();
            mePlayer.Position = m_desiredPosition;
            m_mediaPlayerIsPlaying = true;
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_mediaPlayerIsPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
        }

        public void Pause()
        {
            mePlayer.Pause();
            m_desiredPosition = mePlayer.Position;
            m_mediaPlayerIsPlaying = false;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        public void Stop()
        {
            mePlayer.Stop();
            m_mediaPlayerIsPlaying = false;
        }

        private void SliProgressDragStarted(object sender, DragStartedEventArgs e)
        {
            m_userIsDraggingSlider = true;
        }

        private void SliProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            m_userIsDraggingSlider = false;
            mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void SliProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            mePlayer.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        private void Mute_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Mute_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (mePlayer.IsMuted)
            {
                mePlayer.IsMuted = false;
                MuteButton.Content = "Mute";
            }
            else
            {
                mePlayer.IsMuted = true;
                MuteButton.Content = "Unmute";
            }
        }

        private void MePlayer_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            m_mediaPlayerIsPlaying = false;
            MediaEnded(sender, e);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (Path.GetExtension(SpecifiedFile) == ".mp3")
            {
                Open();
                OnMediaLoaded();
                if (m_mediaPlayerIsPlaying)
                {
                    mePlayer.Play();
                }
            }
            else
            {
                Next();
                if (m_mediaPlayerIsPlaying)
                {
                    mePlayer.Play();
                }   
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Previous();
            if (m_mediaPlayerIsPlaying)
            {
                mePlayer.Play();
            }   
        }

        private void OnMediaLoaded()
        {
            MediaLoaded?.Invoke(this, new EventArgs());
        }
    }
}
