using System;
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

        public MainWindow()
        {
            InitializeComponent();
            replayPalette = new ReplayPalette("COM3");
            replayPalette.ScrubDial.Updated += HandleScrub;
            replayPalette.SpeedSlider.Updated += HandleSpeed;
            replayPalette.OpenButton.Updated += HandleOpenButton;
            replayPalette.ClearButton.Updated += HandleClearButton;
        }

        void Window_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Handled = true;
        }

        void Window_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {

            // Get the Rectangle and its RenderTransform matrix.
            Rectangle rectToMove = e.OriginalSource as Rectangle;
            Matrix rectsMatrix = ((MatrixTransform)rectToMove.RenderTransform).Matrix;

            // Rotate the Rectangle.
            rectsMatrix.RotateAt(e.DeltaManipulation.Rotation,
                                 e.ManipulationOrigin.X,
                                 e.ManipulationOrigin.Y);

            // Resize the Rectangle.  Keep it square 
            // so use only the X value of Scale.
            rectsMatrix.ScaleAt(e.DeltaManipulation.Scale.X,
                                e.DeltaManipulation.Scale.X,
                                e.ManipulationOrigin.X,
                                e.ManipulationOrigin.Y);

            // Move the Rectangle.
            rectsMatrix.Translate(e.DeltaManipulation.Translation.X,
                                  e.DeltaManipulation.Translation.Y);

            // Apply the changes to the Rectangle.
            rectToMove.RenderTransform = new MatrixTransform(rectsMatrix);

            Rect containingRect =
                new Rect(((FrameworkElement)e.ManipulationContainer).RenderSize);

            Rect shapeBounds =
                rectToMove.RenderTransform.TransformBounds(
                    new Rect(rectToMove.RenderSize));

            // Check if the rectangle is completely in the window.
            // If it is not and intertia is occuring, stop the manipulation.
            if (e.IsInertial && !containingRect.Contains(shapeBounds))
            {
                e.Complete();
            }


            e.Handled = true;
        }

        void Window_InertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {

            // Decrease the velocity of the Rectangle's movement by 
            // 10 inches per second every second.
            // (10 inches * 96 pixels per inch / 1000ms^2)
            e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);

            // Decrease the velocity of the Rectangle's resizing by 
            // 0.1 inches per second every second.
            // (0.1 inches * 96 pixels per inch / (1000ms^2)
            e.ExpansionBehavior.DesiredDeceleration = 0.1 * 96 / (1000.0 * 1000.0);

            // Decrease the velocity of the Rectangle's rotation rate by 
            // 2 rotations per second every second.
            // (2 * 360 degrees / (1000ms^2)
            e.RotationBehavior.DesiredDeceleration = 720 / (1000.0 * 1000.0);

            e.Handled = true;
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
