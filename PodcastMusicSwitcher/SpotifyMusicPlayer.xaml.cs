using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using SpotifyAPI.Web;

namespace PodcastMusicSwitcher
{
    public partial class SpotifyMusicPlayer
    {
        private bool m_mediaPlayerIsPlaying;
        private bool m_userIsDraggingSlider;

        public TimeSpan Position => TimeSpan.FromSeconds(sliProgress.Value);

        private TimeSpan m_desiredPosition;
         
        public SpotifyMusicPlayer()
        {
            InitializeComponent();
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

        private async void TimerTick(object sender, EventArgs e)
        {
            if (m_userIsDraggingSlider)
            {
                return;
            }

            sliProgress.Minimum = 0;
            if (m_spotify != null)
            {
                if (m_currentTrack != null)
                {
                    m_currentlyPlaying = await m_spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                    IPlayableItem currentlyPlayingItem = m_currentlyPlaying.Item;
                    m_currentTrack = currentlyPlayingItem as FullTrack;
                    if (m_currentTrack != null)
                    {
                        sliProgress.Maximum = ((double)m_currentTrack.DurationMs / 1000);
                        sliProgress.Value = m_currentlyPlaying.ProgressMs == null ? 0.00 : (double)m_currentlyPlaying.ProgressMs / 1000;
                    }
                }
            }
        }

        public bool IsFinished
        {
            get
            {
                if (m_currentTrack == null)
                {
                    return false;
                }

                return sliProgress.Maximum - sliProgress.Value < 1.5;
            }
        }

        public async void Next()
        {
            if (m_spotify != null)
            {
                await m_spotify.Player.SkipNext();
                await GetCurrentlyPlayingOnSpotify();
            }
        }

        private async void Previous()
        {
            if (m_spotify != null)
            {
                await m_spotify.Player.SkipPrevious();
                await GetCurrentlyPlayingOnSpotify();
            }
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Play();
        }

        private int m_volumePercent;

        public async void Play()
        {
            try
            {
                if (m_spotify == null)
                {
                    if (string.IsNullOrEmpty(AccessTokenTextBox.Text))
                    {
                        MessageBox.Show("You have to supply an access token to be able to connect to Spotify", "AccessToken missing");
                        return;
                    }
                    m_spotify = new SpotifyClient(AccessTokenTextBox.Text);
                    m_volumePercent = 50;
                    pbVolume.Value = m_volumePercent;
                    await m_spotify.Player.SetVolume(new PlayerVolumeRequest(m_volumePercent));
                }

                if (m_spotify != null)
                {
                    await m_spotify.Player.ResumePlayback();
                    await GetCurrentlyPlayingOnSpotify();
                }

                mePlayer.Position = m_desiredPosition;
                m_mediaPlayerIsPlaying = true;
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void ShowError(Exception exception)
        {
            MessageBox.Show(exception.ToString(), "En feil oppstod");
        }

        private FullTrack m_currentTrack;

        private async Task GetCurrentlyPlayingOnSpotify()
        {
            await Task.Delay(300);
            m_currentlyPlaying = await m_spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
            IPlayableItem currentlyPlayingItem = m_currentlyPlaying.Item;
            m_currentTrack = currentlyPlayingItem as FullTrack;
            if (m_currentTrack != null)
            {
                SongTitleLabel.Content = m_currentTrack.Name;

                string currentArtists = string.Empty;
                foreach (SimpleArtist currentArtist in m_currentTrack.Artists)
                {
                    currentArtists += currentArtist.Name + ", ";
                }

                ArtistLabel.Content = currentArtists.Remove(currentArtists.Length - 2, 1);
                AlbumLabel.Content = m_currentTrack.Album.Name;
                lblDuration.Text = new TimeSpan(0, 0, 0, 0, m_currentTrack.DurationMs).ToString(@"hh\:mm\:ss");
            }
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_mediaPlayerIsPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
        }

        public async void Pause()
        {
            m_mediaPlayerIsPlaying = false;
            if (m_spotify != null)
            {
                await m_spotify.Player.PausePlayback();
            }
            m_desiredPosition = mePlayer.Position;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        public async void Stop()
        {
            if (m_spotify != null)
            {
                await m_spotify.Player.PausePlayback();
            }
            m_mediaPlayerIsPlaying = false;
        }

        private void SliProgressDragStarted(object sender, DragStartedEventArgs e)
        {
            m_userIsDraggingSlider = true;
        }

        private void SliProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            m_userIsDraggingSlider = false;
            m_spotify?.Player.SeekTo(new PlayerSeekToRequest((long)(sliProgress.Value * 1000)));
        }

        private void SliProgressValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_spotify != null)
            {
                m_volumePercent += (e.Delta > 0) ? 1 : -1;
                if (m_volumePercent > 100)
                {
                    m_volumePercent = 100;
                }

                if (m_volumePercent < 0)
                {
                    m_volumePercent = 0;
                }

                pbVolume.Value = m_volumePercent;
                m_spotify.Player.SetVolume(new PlayerVolumeRequest(m_volumePercent));
            }
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
                m_spotify?.Player.SetVolume(new PlayerVolumeRequest(50));
            }
            else
            {
                mePlayer.IsMuted = true;
                MuteButton.Content = "Unmute";
                m_spotify?.Player.SetVolume(new PlayerVolumeRequest(0));
            }
        }

        private void MePlayer_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            m_mediaPlayerIsPlaying = false;
            MediaEnded(sender, e);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Previous();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private SpotifyClient m_spotify;
        private CurrentlyPlaying m_currentlyPlaying;
    }
}
