using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace PodcastMusicSwitcher
{
    public partial class MainWindow
    {
        public int SwitchIntervalInSeconds = 240;

        private DateTime m_podcastStarted;
        private bool m_songPlaying;
        private bool m_podcastPlaying;
        private bool m_isPaused;
        private TimeSpan m_podcastPlayTime;
        private const string m_configFileName = "config.xml";

        public MainWindow()
        {
            InitializeComponent();
            
            PodcastPlayer.DefaultPath = @"D:\Music\Radioresepsjonen";
            PodcastPlayer.FileFilter = "Media files (*.mp3)|*.mp3|All files (*.*)|*.*";
            SongPlayer.DefaultPath = @"D:\Music\Spillelister";
            SongPlayer.FileFilter = "Windows Media Player playlists(*.wpl)|*.wpl|All files (*.*)|*.*";

            PodcastPlayer.MediaLoaded += PodcastPlayer_MediaLoaded;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += TimerTick;
            timer.Start();
        }

        void PodcastPlayer_MediaLoaded(object sender, EventArgs e)
        {
            m_podcastStarted = DateTime.Now;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (m_podcastPlaying && !m_isPaused && DateTime.Now - m_podcastStarted > TimeSpan.FromSeconds(SwitchIntervalInSeconds))
            {
                SwitchToMusic();
            }

            if (m_songPlaying && !m_isPaused && SongPlayer.IsFinished)
            {
                SwitchToPodcast();
            }
        }

        private void SwitchToMusic()
        {
            PodcastPlayer.Pause();
            m_podcastPlaying = false;
            m_songPlaying = true;
            SongPlayer.Play();
            m_podcastStarted = DateTime.Now;
            SwitchButton.Content = "Bytt til podkast";
        }

        private void SongEnded(object sender, EventArgs e)
        {
            SwitchToPodcast();
        }

        private void SwitchToPodcast()
        {
            m_podcastStarted = DateTime.Now;
            m_podcastPlaying = true;
            m_songPlaying = false;
            PodcastPlayer.Play();
            SongPlayer.Pause();
            //SongPlayer.Next();
            SwitchButton.Content = "Bytt til musikk";
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            m_podcastStarted = DateTime.Now;
            m_podcastPlaying = true;
            m_songPlaying = false;
            PodcastPlayer.Play();
            StartButton.IsEnabled = false;
            SwitchButton.IsEnabled = true;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PauseAll();
        }

        private void PauseAll()
        {
            if (m_isPaused == false)
            {
                m_isPaused = true;
                PauseButton.Content = "Continue";
                SwitchButton.IsEnabled = false;
                if (m_podcastPlaying)
                {
                    m_podcastPlayTime = DateTime.Now - m_podcastStarted;
                    PodcastPlayer.Pause();
                }
                else
                {
                    SongPlayer.Pause();
                }
            }
            else
            {
                m_isPaused = false;
                PauseButton.Content = "Pause";
                SwitchButton.IsEnabled = true;
                if (m_podcastPlaying)
                {
                    m_podcastStarted = DateTime.Now.AddSeconds(-m_podcastPlayTime.TotalSeconds);
                    PodcastPlayer.Play();
                }
                else
                {
                    SongPlayer.Play();
                }
            }
        }

        private void PodcastEnded(object sender, EventArgs e)
        {
            PauseAll();
            MessageBoxResult result = MessageBox.Show("Podcasten er slutt." + Environment.NewLine + "Vil du starte en ny?", "Podcast er slutt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PodcastPlayer.Open();
            }
            else
            {
                Close();
            }
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            var rootElement = new XElement("config");
           
            rootElement.Add(new XElement("PodcastFile", PodcastPlayer.SpecifiedFile));
            rootElement.Add(new XElement("PodcastPosition", Math.Floor(PodcastPlayer.Position.TotalSeconds)));
            rootElement.Add(new XElement("PodcastDefaultPath", PodcastPlayer.DefaultPath));
            rootElement.Add(new XElement("PodcastFileFilter", PodcastPlayer.FileFilter));
            //rootElement.Add(new XElement("SongPlayerShuffle", SongPlayer.ShuffleCheckBox.IsChecked));
            rootElement.Add(new XElement("SongPlaylist", SongPlayer.SpecifiedFile));
            rootElement.Add(new XElement("SongPlaying", SongPlayer.FilePlaying));
            rootElement.Add(new XElement("SongDefaultPath", SongPlayer.DefaultPath));
            rootElement.Add(new XElement("SongFileFilter", SongPlayer.FileFilter));
            rootElement.Add(new XElement("SongPosition", Math.Floor(SongPlayer.Position.TotalSeconds)));
         //   rootElement.Add(new XElement("SwitchIntervalInSeconds", SwitchIntervalInSeconds));
            rootElement.Add(new XElement("AccessToken", SongPlayer.AccessTokenTextBox.Text));

            File.WriteAllText(m_configFileName, rootElement.ToString());

            SongPlayer.Stop();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(m_configFileName))
            {
                var config = new XElement("config");
                try
                {
                    string configText = File.ReadAllText(m_configFileName);
                    config = XElement.Parse(configText);
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Kunne ikke lese config-fil");
                }
                try
                {
                    foreach (XElement el in config.Elements())
                    {
                        if (string.IsNullOrEmpty(el.Value))
                        {
                            continue;
                        }
                        switch (el.Name.ToString())
                        {
                            case "PodcastFile":
                                PodcastPlayer.LoadFile(el.Value);
                                break;
                            case "PodcastPosition":
                                PodcastPlayer.SetPosition(new TimeSpan(0, 0, Int32.Parse(el.Value)));
                                break;
                            //case "SongPlaylist":
                            //    SongPlayer.LoadFile(el.Value);
                            //    break;
                            //case "SongPlaying":
                            //    SongPlayer.FilePlaying = el.Value; 
                            //    break;
                            //case "SongPosition":
                            //    SongPlayer.SetPosition(new TimeSpan(0, 0, Int32.Parse(el.Value)));
                            //    break;
                            //case "SongPlayerShuffle":
                            //    SongPlayer.ShuffleCheckBox.IsChecked = bool.Parse(el.Value);
                            //    break;
                            case "PodcastDefaultPath":
                                PodcastPlayer.DefaultPath = el.Value;
                                break;
                            //case "SongDefaultPath":
                            //    SongPlayer.DefaultPath = el.Value;
                            //    break;
                            //case "SongPlayerFileFilter":
                            //    SongPlayer.FileFilter = el.Value;
                            //    break;
                            case "PodcastFileFilter":
                                PodcastPlayer.FileFilter = el.Value;
                                break;
                            //case "SwitchIntervalInSeconds":
                            //    SwitchIntervalInSeconds = int.Parse(el.Value);
                            //    break;
                            case "AccessToken":
                                SongPlayer.AccessTokenTextBox.Text = el.Value;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Kunne ikke laste config-fil");
                }
            }
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_podcastPlaying)
            {
                SwitchToMusic();
            }
            else
            {
                SwitchToPodcast();
            }
        }
    }
}
