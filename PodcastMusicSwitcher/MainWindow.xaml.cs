﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace PodcastMusicSwitcher
{
    public partial class MainWindow
    {
        public int SwitchIntervalInSeconds = 3;

        private DateTime m_podcastStarted;
        private bool m_songPlaying;
        private bool m_podcastPlaying;
        private bool m_isPaused;
        private TimeSpan m_podcastPlayTime;
        private int m_switchTimeIndex;
        private const string m_configFileName = "config.xml";

        public MainWindow()
        {
            InitializeComponent();
            
            PodcastPlayer.DefaultPath = @"D:\Music\Radioresepsjonen";
            PodcastPlayer.FileFilter = "Media files (*.mp3)|*.mp3|All files (*.*)|*.*";
            
            PodcastPlayer.MediaLoaded += PodcastPlayer_MediaLoaded;
            m_switchTimeIndex = 0;
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
            m_switchTimeIndex = ChangeTimesComboBox.SelectedIndex;
            if (m_podcastPlaying && !m_isPaused && (PodcastPlayer.Position.TotalSeconds >= (int)((TimeSpan)ChangeTimesComboBox.Items[m_switchTimeIndex]).TotalSeconds))
            {
                m_switchTimeIndex++;
                ChangeTimesComboBox.SelectedIndex = m_switchTimeIndex;
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
 
        private void SwitchToPodcast()
        {
            m_podcastStarted = DateTime.Now;
            m_podcastPlaying = true;
            m_songPlaying = false;
            PodcastPlayer.Play();
            SongPlayer.Pause();
            SwitchButton.Content = "Bytt til musikk";
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateChangedTimesComboBoxSelectedIndex();
            m_podcastStarted = DateTime.Now;
            m_podcastPlaying = true;
            m_songPlaying = false;
            PodcastPlayer.Play();
            StartButton.IsEnabled = false;
            SwitchButton.IsEnabled = true;
        }

        private void CalculateChangedTimesComboBoxSelectedIndex()
        {
            int i = 0;
            while ((int)((TimeSpan)ChangeTimesComboBox.Items[i]).TotalSeconds <= PodcastPlayer.Position.TotalSeconds)
            {
                i++;
            }

            ChangeTimesComboBox.SelectedIndex = i;
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
            rootElement.Add(new XElement("SongPosition", Math.Floor(SongPlayer.Position.TotalSeconds)));
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
                                PodcastPlayer.SetPosition(new TimeSpan(0, 0, int.Parse(el.Value)));
                                break;
                            case "PodcastDefaultPath":
                                PodcastPlayer.DefaultPath = el.Value;
                                break;
                            case "PodcastFileFilter":
                                PodcastPlayer.FileFilter = el.Value;
                                break;
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

                int durationSeconds = (int)PodcastPlayer.Duration.TotalSeconds;
                for (int i = 0; i < durationSeconds; i += SwitchIntervalInSeconds)
                {
                    if (i > 0)
                    {
                        ChangeTimesComboBox.Items.Add(new TimeSpan(0, 0, 0, i));
                    }
                }

                ChangeTimesComboBox.SelectedIndex = 0;
                CalculateChangedTimesComboBoxSelectedIndex();
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

        private void GetFromNrkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var nrkReadWindow = new NrkReadWindow(this);
            nrkReadWindow.Show();
        }
    }
}
