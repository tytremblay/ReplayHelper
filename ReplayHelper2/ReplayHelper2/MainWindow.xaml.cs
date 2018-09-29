using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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
        private bool fileOpen = false;
        private double scrubCount = 0.0;

        public MainWindow()
        {
            InitializeComponent();
            replayPalette = new ReplayPalette("COM3");
            replayPalette.ScrubDial.Updated += HandleScrub;
            replayPalette.SpeedSlider.Updated += HandleSpeed;
            replayPalette.OpenButton.Updated += HandleOpenButton;
            replayPalette.ClearButton.Updated += HandleClearButton;

            drawingCanvas.DefaultDrawingAttributes.Color = (Color)ColorConverter.ConvertFromString("#FFCC00");
            drawingCanvas.DefaultDrawingAttributes.Height = 10;
            drawingCanvas.DefaultDrawingAttributes.Width = 10;

            CreateServer();
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
                    if (!this.fileOpen)
                    {
                        OpenLatestVideo();
                    }
                    else
                    {
                        CloseVideo();
                    }
                }
            });
        }

        private void HandleSpeed(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                MediaPlayer.SpeedRatio = replayPalette.getPlaybackSpeed();
            });
        }

        private void HandleScrub(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (fileOpen)
                {
                    double videoSeconds = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    double scaleFactor = 720.0;
                    double scrubSpeed = replayPalette.ScrubDial.Speed * (videoSeconds/scaleFactor);
                    if (scrubSpeed != 0.0)
                    {
                        if (isPlaying)
                        {
                            Pause();
                        }
                        scrubCount += scrubSpeed;
                        Console.WriteLine($"scrubSpeed: {scrubSpeed}");
                        Console.WriteLine($"scrubCount: {scrubCount}");
                        MediaPlayer.Position += TimeSpan.FromSeconds(scrubSpeed);
                    }
                    if (replayPalette.ScrubDial.Button.JustPressed)
                    {
                        TogglePlayPause();
                    }
                }
            });
        }

        private void Pause()
        {
            isPlaying = false;
            MediaPlayer.Pause();
        }

        private void Play()
        {
            isPlaying = true;
            MediaPlayer.Play();
        }

        private void TogglePlayPause()
        {
            if (isPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void ClearDrawings()
        {
            drawingCanvas.Strokes.Clear();
            TelePropmtBox.Text = string.Empty;
        }

        private void CloseVideo()
        {
            MediaPlayer.Source = null;
            fileOpen = false;
            ReplayLabel.Visibility = Visibility.Hidden;
        }

        private void OpenLatestVideo()
        {

            DirectoryInfo replayDirectory = new DirectoryInfo(@"C:\\ReplayFiles\OnStage");

            var files = replayDirectory.GetFiles().OrderByDescending(p => p.CreationTime).ToList();
            FileInfo replayFile = files.First();
            MediaPlayer.Source = new Uri(replayFile.FullName);
            MediaPlayer.Pause();
            MediaPlayer.IsMuted = true;
            MediaPlayer.Position = TimeSpan.FromSeconds(0.5);
            fileOpen = true;
            ClearDrawings();
            ReplayLabel.Visibility = Visibility.Visible;
            /*
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
                MediaPlayer.Pause();
                MediaPlayer.Position = TimeSpan.FromSeconds(0.5);
            }
            */
        }

        private void CreateServer()
        {
            var tcp = new TcpListener(IPAddress.Any, 40190);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        string incomming;
                        byte[] bytes = new byte[1024];


                        int i = stream.Read(bytes, 0, bytes.Length);
                        incomming = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        this.Dispatcher.Invoke(() =>
                        {
                            TelePropmtBox.Text = incomming;
                        });
                        tcpClient.Close();
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }
    }
}
