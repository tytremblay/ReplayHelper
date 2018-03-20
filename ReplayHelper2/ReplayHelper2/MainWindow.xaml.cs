using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SlimDX.DirectInput;
using SlimDX.RawInput;
using System.Windows.Threading;
using ReplayHelper2.PaletteGear;

namespace ReplayHelper2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        Joystick[] joysticks;
        ReplayPalette replayPalette = null;
        int lastScrubVal = 0;
        double lastPlaybackSpeed = 1.0;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            joysticks = GetJoysticks();
            replayPalette = new ReplayPalette(joysticks[0]);
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(5);
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {            
            handleReplayPallete(replayPalette);
        }

        private void handleReplayPallete(ReplayPalette rp)
        {
            rp.Refresh();
            Console.WriteLine(rp.ToString());
            if (rp.OpenButton.JustPressed)
            {
                openLatestVideo();
            }

            if (rp.PlayPauseButton.JustPressed)
            {
                if (isPlaying)
                {
                    MediaPlayer.Pause();
                    isPlaying = false;
                }
                else
                {
                    MediaPlayer.Play();
                    isPlaying = true;
                }
                
            }

            if (rp.playbackSpeed != lastPlaybackSpeed)
            {
                MediaPlayer.SpeedRatio = rp.playbackSpeed;
                lastPlaybackSpeed = rp.playbackSpeed;
            }
        }

        public Joystick[] GetJoysticks()
        {
            DirectInput input = new DirectInput();
            Joystick stick;
            List<Joystick> sticks = new List<Joystick>();

            foreach( DeviceInstance device in input.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    stick = new Joystick(input, device.InstanceGuid);
                    stick.Acquire();

                    IList<DeviceObjectInstance> objects = stick.GetObjects();

                    foreach(DeviceObjectInstance deviceObject in objects)
                    {
                        if (deviceObject.ObjectType == ObjectDeviceType.AbsoluteAxis)
                        {
                            stick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
                        }
                        else if (deviceObject.ObjectType == ObjectDeviceType.RelativeAxis)
                        {
                            stick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-10000, 10000);
                        }
                    }
                    sticks.Add(stick);
                }
                catch(DirectInputException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return sticks.ToArray();
        }

        private void openLatestVideo()
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
