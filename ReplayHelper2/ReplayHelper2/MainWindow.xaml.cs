using System;
using System.Windows;
using ReplayHelper2.PaletteGear;

namespace ReplayHelper2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ReplayPalette replayPalette = null;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            replayPalette = new ReplayPalette("COM3");
            replayPalette.ScrubDial.Updated += HandleScrub;
            replayPalette.SpeedSlider.Updated += HandleSpeed;
            replayPalette.OpenButton.Updated += HandleOpenButton;
            replayPalette.ClearButton.Updated += HandleClearButton;
        }

        private void HandleClearButton(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                ClearDrawings();
            });
        }

        private void HandleOpenButton(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (replayPalette.OpenButton.JustPressed)
                {
                    OpenLatestVideo();
                }
            });
        }

        private void HandleSpeed(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                MediaPlayer.SpeedRatio = replayPalette.getPlaybackSpeed();
                if (MediaPlayer.SpeedRatio == 1.0)
                {
                    MediaPlayer.IsMuted = false;
                }
                else
                {
                    MediaPlayer.IsMuted = true;
                }
            });
        }

        private void HandleScrub(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                double scrubSpeed = replayPalette.ScrubDial.Speed * .25;
                if (scrubSpeed != 0.0)
                {
                    Pause(true);
                    MediaPlayer.Position += TimeSpan.FromSeconds(scrubSpeed);
                }
                if (replayPalette.ScrubDial.Button.JustPressed)
                {
                    TogglePlayPause(true);
                }
            });
        }

        private void Pause(bool mute)
        {
            isPlaying = false;
            if (mute)
            {
                MediaPlayer.IsMuted = true;
            }
            MediaPlayer.Pause();
        }

        private void Play(bool unMute)
        {
            isPlaying = true;
            if (unMute)
            {
                MediaPlayer.IsMuted = false;
            }
            MediaPlayer.Play();
        }

        private void TogglePlayPause(bool toggleMute)
        {
            if (isPlaying)
            {
                Pause(toggleMute);
            }
            else
            {
                Play(toggleMute);
            }
        }

        private void ClearDrawings()
        {

        }

        private void OpenLatestVideo()
        {
            // Configure open file dialog box 
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "Videos"; // Default file name 
            dialog.DefaultExt = ".mov"; // Default file extension 
            dialog.Filter = "MOV file (.mov)|*.mov"; // Filter files by extension  

            // Show open file dialog box 
            Nullable<bool> result = dialog.ShowDialog();

            // Process open file dialog box results  
            if (result == true)
            {
                // Open document  
                MediaPlayer.Source = new Uri(dialog.FileName);
            }
        }
    }
}
