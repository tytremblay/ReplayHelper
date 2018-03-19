using System;
using System.Linq;
using Windows.Foundation;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Media;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.System;
using Windows.UI;
using Windows.Storage;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Devices.Sensors;
using Windows.Storage.Streams;
using Windows.Storage.FileProperties;
using Windows.Graphics.Imaging;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Windows.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;
using Windows.Storage.Search;
using Windows.UI.Input;
using System.Collections.Generic;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ReplayHelper
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Create this variable at a global scope. Set it to null.
        private readonly DisplayRequest appDisplayRequest = new DisplayRequest();
        DeviceInformation cameraDevice;
        DeviceInformationCollection allCameras;
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _isRecording;
        private bool _externalCamera;
        private bool _mirroringPreview;

        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;

        private readonly SystemMediaTransportControls _systemMediaControls = SystemMediaTransportControls.GetForCurrentView();

        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        public object Keys { get; private set; }

        private StorageFile latestRecordingFile = null;
        private StorageFolder currentFolder;

        // Stroke selection tool.
        private Polyline drawnPolyLine;
        private Line drawnLine;
        private EllipseGeometry drawnCircle;

        private Color currentColor;
        private double lineThickness = 10;

        bool lineDrawingMode = false;
        bool circleDrawingMode = false;
        bool shapeOnCanvas = false;
        bool manipulating = false;

        private DispatcherTimer stopWatch;
        private double numSeconds = 0.0;
        private double prevPlaybackRate = 1.0;

        private int numActiveContacts = 0;
        private int prevNumActiveContacts = 0;
        private double twoFingerDragXStart = 0;
        private double posAtLastPlaybackRateChange = 0;
        private TimeSpan mediaPlayerScrubStartTime;
        private bool scrubMode = false;
        private bool timeAdjustMode = false;

        public MainPage()
        {
            this.InitializeComponent();

            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;

            currentColor = Colors.White;

            FreeDrawButton.Background = new SolidColorBrush(Colors.LightGray);
            FreeDrawButton.Foreground = new SolidColorBrush(Colors.Black);

            stopWatch = new DispatcherTimer();
            stopWatch.Interval = new TimeSpan(0, 0, 0, 0, 100);
            stopWatch.Tick += StopWatch_Tick;

            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);

            if (ctrl.HasFlag(CoreVirtualKeyStates.None))
            {
                scrubMode = false;
            }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            {
                scrubMode = true;
                switch (e.VirtualKey)
                {
                    case VirtualKey.Z: UndoButton_Click(UndoButton, new RoutedEventArgs()); break;
                    case VirtualKey.O: openButton_Click(openFileButton, new RoutedEventArgs()); break;
                    case VirtualKey.S: recordButton_Click(recordButton, new RoutedEventArgs()); break;
                }
            }
            else
            {
                switch (e.VirtualKey)
                {
                    case VirtualKey.R: OnPenColorChanged(RedPenButton, new RoutedEventArgs()); break;
                    case VirtualKey.Q: OnPenColorChanged(YellowPenButton, new RoutedEventArgs()); break;
                    case VirtualKey.E: OnPenColorChanged(BluePenButton, new RoutedEventArgs()); break;
                    case VirtualKey.W: OnPenColorChanged(WhitePenButton, new RoutedEventArgs()); break;
                    case VirtualKey.Z: FreeDrawButton_Click(FreeDrawButton, new RoutedEventArgs()); break;
                    case VirtualKey.X: DrawLinesButton_Click(DrawLinesButton, new RoutedEventArgs()); break;
                    case VirtualKey.C: DrawCircleButton_Click(DrawCircleButton, new RoutedEventArgs()); break;
                    case VirtualKey.Escape: ClearDrawingButton_Click(ClearDrawingButton, new RoutedEventArgs()); break;
                    case VirtualKey.Space: PlayPauseToggle(); break;
                    case VirtualKey.Up: IncreasePlaybackRate(); break;
                    case VirtualKey.Down: DecreasePlaybackRate(); break;
                    case VirtualKey.Left: ScrubVideo(-5.0); break;
                    case VirtualKey.Right: ScrubVideo(5.0); break;

                }
            }
        }

        public void ScrubVideo(double seconds)
        {
            double millisecondsToScrub = seconds * 1000;
            mediaPlayer.Position += TimeSpan.FromMilliseconds(millisecondsToScrub);
        }

        public void IncreasePlaybackRate()
        {

            if (mediaPlayer.PlaybackRate < 4)
            {
                mediaPlayer.PlaybackRate *= 2;
            }

            PlaybackRateButton.Content = String.Format("{0}x", mediaPlayer.PlaybackRate);
        }

        public void DecreasePlaybackRate()
        {
            if (mediaPlayer.PlaybackRate > .125)
            {
                mediaPlayer.PlaybackRate /= 2;
            }

            PlaybackRateButton.Content = String.Format("{0}x", mediaPlayer.PlaybackRate);
        }

        private void PlayPauseToggle()
        {
            if (mediaPlayer.CurrentState != MediaElementState.Playing)
            {
                mediaPlayer.Play();
                if (timerText.Visibility != Visibility.Collapsed)
                {
                    stopWatch.Start();
                }
                Debug.WriteLine("Playback Rate: " + mediaPlayer.PlaybackRate);
            }
            else {
                mediaPlayer.Pause();
                stopWatch.Stop();
            }
        }

        private void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                UnregisterOrientationEventHandlers();

                deferral.Complete();
            }
        }

        private void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                RegisterOrientationEventHandlers();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RegisterOrientationEventHandlers();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            UnregisterOrientationEventHandlers();

        }

        private async void ShowMessageToUser(string message)
        {
            var dialog = new MessageDialog(message);

            var result = await dialog.ShowAsync();
        }


        private void RegisterOrientationEventHandlers()
        {
            // If there is an orientation sensor present on the device, register for notifications
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            _displayOrientation = _displayInformation.CurrentOrientation;


        }

        private void UnregisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

        }

        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        private static int ConvertDeviceOrientationToDegrees(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return 90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return 180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return 270;
                case SimpleOrientation.NotRotated:
                default:
                    return 0;
            }
        }

        
        async void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        mediaPlayer.Play();
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        mediaPlayer.Pause();
                    });
                    break;
                default:
                    break;
            }
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyout m = new MenuFlyout();

            MenuFlyoutItem setFolder = new MenuFlyoutItem();
            setFolder.Text = "Set Folder";
            setFolder.Tapped += openButton_Mn_Tapped;
            m.Items.Add(setFolder);

            MenuFlyoutItem openFIle = new MenuFlyoutItem();
            openFIle.Text = "Open File";
            openFIle.Tapped += openButton_Mn_Tapped;
            m.Items.Add(openFIle);

            m.ShowAt((FrameworkElement)sender);

            //await SetLocalMedia();
        }

        async private System.Threading.Tasks.Task SetLocalMedia()
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            openPicker.FileTypeFilter.Add(".wmv");
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".wma");
            openPicker.FileTypeFilter.Add(".mp3");

            var file = await openPicker.PickSingleFileAsync();

            // mediaPlayer is a MediaElement defined in XAML
            if (file != null)
            {
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                mediaPlayer.SetSource(stream, file.ContentType);

                mediaPlayer.Play();
                mediaPlayer.IsMuted = true;

            }
        }

        async private System.Threading.Tasks.Task SetFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();

            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;

            folderPicker.FileTypeFilter.Add(".wmv");
            folderPicker.FileTypeFilter.Add(".mp4");
            folderPicker.FileTypeFilter.Add(".wma");
            folderPicker.FileTypeFilter.Add(".mp3");
            folderPicker.FileTypeFilter.Add(".mov");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                currentFolder = folder;
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void mediaPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            MediaElement mediaElement = sender as MediaElement;
            if (mediaElement != null && mediaElement.IsAudioOnly == false)
            {
                if (mediaElement.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
                {
                    if (appDisplayRequest == null)
                    {
                        // This call creates an instance of the DisplayRequest object. 
                        //appDisplayRequest = new DisplayRequest();
                        //appDisplayRequest.RequestActive();
                    }
                }
                else // CurrentState is Buffering, Closed, Opening, Paused, or Stopped. 
                {
                    if (appDisplayRequest != null)
                    {
                        // Deactivate the display request and set the var to null.
                        //appDisplayRequest.RequestRelease();
                        //appDisplayRequest = null;
                    }
                }
            }
        }

        private void ClearDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            clearShapeCanvas();
            setTimerVisibility(Visibility.Collapsed, true);
            mediaPlayer.PlaybackRate = 1;

        }


        private void clearShapeCanvas()
        {
            shapeCanvas.Children.Clear();
            shapeCanvas.InvalidateArrange();
        }

        private void undoLastInk()
        {
            shapeCanvas.Children.Remove(shapeCanvas.Children.LastOrDefault());
        }

        // Update ink stroke color for new strokes.
        private void OnPenColorChanged(object sender, RoutedEventArgs e)
        {
            Button buttonPressed = sender as Button;

            RedPenButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            YellowPenButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            WhitePenButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            BluePenButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

            buttonPressed.Background = new SolidColorBrush(Colors.LightGray);

            switch (buttonPressed.Name.Replace("PenButton", ""))
            {
                case "Blue":
                    currentColor = Colors.Blue;
                    break;
                case "Red":
                    currentColor = Colors.Red;
                    break;
                case "Yellow":
                    currentColor = Colors.Yellow;
                    break;
                case "White":
                    currentColor = Colors.White;
                    break;
                default:
                    currentColor = Colors.White;
                    break;
            };
        }


        private void MediaTransportControls_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine("Double Tap Captured!");
        }

        private async void recordButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (recordHelpText.Text == "Stop")
            {

                if (latestRecordingFile != null)
                {
                    var stream = await latestRecordingFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    mediaPlayer.SetSource(stream, latestRecordingFile.ContentType);

                    mediaPlayer.Play();

                }
            }
            else
            {
                clearShapeCanvas();
            }

        }

        private void recordButton_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            MenuFlyout m = new MenuFlyout();
            MenuFlyoutItem description = new MenuFlyoutItem();
            description.Text = "Choose Camera";

            m.Items.Add(description);
            m.Items.Add(new MenuFlyoutSeparator());

            foreach (DeviceInformation d in allCameras)
            {
                MenuFlyoutItem mn = new MenuFlyoutItem();
                mn.Text = d.Name;
                mn.Tapped += Mn_Tapped;
                m.Items.Add(mn);
            }

            m.ShowAt((FrameworkElement)sender);
        }

        private async void openButton_Mn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MenuFlyoutItem mfi = sender as MenuFlyoutItem;

            switch (mfi.Text)
            {
                case "Set Folder":
                    await SetFolder();
                    break;
                case "Open File":
                    await SetLocalMedia();
                    break;
                default:
                    break;
            }
        }

        private void Mn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MenuFlyoutItem mfi = sender as MenuFlyoutItem;
            cameraDevice = allCameras.FirstOrDefault(x => x.Name == mfi.Text);
        }
        

        private void shapeCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

            var canvas = (Canvas)sender;

            canvas.CapturePointer(e.Pointer);
            prevNumActiveContacts = numActiveContacts;
            ++numActiveContacts;
            Debug.WriteLine("numActiveContacts: " + numActiveContacts);

            //var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);

            if (scrubMode)
            {
                mediaPlayerScrubStartTime = mediaPlayer.Position;
                twoFingerDragXStart = e.GetCurrentPoint(canvas).Position.X;
                prevPlaybackRate = mediaPlayer.PlaybackRate;
                mediaPlayer.PlaybackRate = 0.0;
            }
            else if (numActiveContacts == 1 && prevNumActiveContacts < 2)
            {
                if (lineDrawingMode)
                {
                    var startPoint = e.GetCurrentPoint(canvas);

                    drawnLine = new Line
                    {
                        Stroke = new SolidColorBrush(currentColor),
                        StrokeThickness = lineThickness,
                        X1 = startPoint.Position.X,
                        Y1 = startPoint.Position.Y,
                        X2 = startPoint.Position.X,
                        Y2 = startPoint.Position.Y,
                        StrokeEndLineCap = PenLineCap.Triangle
                    };

                    
                }
                else if (circleDrawingMode)
                {
                    var center = e.GetCurrentPoint(canvas);

                    drawnCircle = new EllipseGeometry
                    {
                        Center = center.Position,
                        RadiusX = 5,
                        RadiusY = 5
                    };

                    
                }
                else
                {
                    drawnPolyLine = new Polyline()
                    {
                        Stroke = new SolidColorBrush(currentColor),
                        StrokeThickness = 10,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };

                    //drawnPolyLine.Points.Add(e.GetCurrentPoint(canvas).Position);

                    //shapeCanvas.Children.Add(drawnPolyLine);
                }
            }
            else if (numActiveContacts == 2)
            {
                scrubMode = true;
                mediaPlayerScrubStartTime = mediaPlayer.Position;
                twoFingerDragXStart = e.GetCurrentPoint(canvas).Position.X;
                prevPlaybackRate = mediaPlayer.PlaybackRate;
                mediaPlayer.PlaybackRate = 0.0;
            }

        }

        private void shapeCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var canvas = (Canvas)sender;
            ((Canvas)sender).ReleasePointerCapture(e.Pointer);

            prevNumActiveContacts = numActiveContacts;
            if (numActiveContacts > 0)
            {
                --numActiveContacts;
            }
            if (numActiveContacts == 0)
            {
                if (!shapeOnCanvas && !manipulating)
                {
                    PlayPauseToggle();
                }
                else {
                    if (scrubMode)
                    {
                        mediaPlayer.PlaybackRate = prevPlaybackRate;
                    }
                    shapeOnCanvas = false;
                    manipulating = false;
                }
            }
        }



        private void shapeCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var canvas = (Canvas)sender;

            if (canvas.CapturePointer(e.Pointer))
            {
                if (scrubMode)
                {

                    //twoFingerDragXStart = e.GetCurrentPoint(canvas).Position.X;
                    double regionPercentage = (e.GetCurrentPoint(canvas).Position.X - twoFingerDragXStart) / canvas.ActualWidth;
                    double millisecondsToScrub = mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * regionPercentage * mediaPlayer.PlaybackRate;
                    Debug.WriteLine("Scrubbing (milliseconds): " + millisecondsToScrub);
                    mediaPlayer.Position = mediaPlayerScrubStartTime + (TimeSpan.FromMilliseconds(millisecondsToScrub));
                }
                
                else if (numActiveContacts == 1 && prevNumActiveContacts < 2)
                {
                    if (lineDrawingMode)
                    {
                        if (e.GetCurrentPoint(canvas).Position.X != drawnLine.X1 && e.GetCurrentPoint(canvas).Position.Y != drawnLine.Y1)
                        {
                            var endPoint = e.GetCurrentPoint(canvas);
                            drawnLine.X2 = endPoint.Position.X;
                            drawnLine.Y2 = endPoint.Position.Y;
                            if (!shapeOnCanvas)
                            {
                                canvas.Children.Add(drawnLine);
                                shapeOnCanvas = true;
                            }
                        }

                    }
                    else if (circleDrawingMode)
                    {
                        var radiusPoint = e.GetCurrentPoint(canvas).Position;
                        if (radiusPoint.X != drawnCircle.Center.X && radiusPoint.Y != drawnCircle.Center.Y)
                        {
                            double radius = Math.Sqrt(square((radiusPoint.X - drawnCircle.Center.X)) + square((radiusPoint.Y - drawnCircle.Center.Y)));
                            drawnCircle.RadiusX = radius;
                            drawnCircle.RadiusY = radius;
                            if (!shapeOnCanvas)
                            {
                                Path p = new Path();
                                p.Stroke = new SolidColorBrush(currentColor);
                                p.StrokeThickness = lineThickness;


                                p.Data = drawnCircle;
                                canvas.Children.Add(p);

                                shapeOnCanvas = true;
                            }
                        }
                    }
                    else
                    {
                        if (drawnPolyLine.Points.Count > 0)
                        {
                            if (drawnPolyLine.Points.Last() != e.GetCurrentPoint(canvas).Position)
                            {
                                drawnPolyLine.Points.Add(e.GetCurrentPoint(canvas).Position);
                            }
                        }
                        else
                        {
                            drawnPolyLine.Points.Add(e.GetCurrentPoint(canvas).Position);
                        }
                        //Debug.WriteLine("drawnPolyLine Points: " + drawnPolyLine.Points.Count);
                        if (drawnPolyLine.Points.Count > 2 && !shapeOnCanvas)
                        {
                            shapeCanvas.Children.Add(drawnPolyLine);
                            shapeOnCanvas = true;
                        }
                    }
                }
                else if (numActiveContacts == 2)
                {
                    

                }
            }
        }

        private double square(double d)
        {
            return d * d;
        }

        private void DrawLinesButton_Click(object sender, RoutedEventArgs e)
        {
            lineDrawingMode = true;
            circleDrawingMode = false;
            DrawLinesButton.Background = new SolidColorBrush(Colors.LightGray);
            DrawLinesButton.Foreground = new SolidColorBrush(Colors.Black);

            FreeDrawButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            FreeDrawButton.Foreground = new SolidColorBrush(Colors.White);

            DrawCircleButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            DrawCircleButton.Foreground = new SolidColorBrush(Colors.White);


        }

        private void FreeDrawButton_Click(object sender, RoutedEventArgs e)
        {
            lineDrawingMode = false;
            circleDrawingMode = false;

            FreeDrawButton.Background = new SolidColorBrush(Colors.LightGray);
            FreeDrawButton.Foreground = new SolidColorBrush(Colors.Black);

            DrawLinesButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            DrawLinesButton.Foreground = new SolidColorBrush(Colors.White);

            DrawCircleButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            DrawCircleButton.Foreground = new SolidColorBrush(Colors.White);
        }

        private void DrawCircleButton_Click(object sender, RoutedEventArgs e)
        {
            lineDrawingMode = false;
            circleDrawingMode = true;

            DrawCircleButton.Background = new SolidColorBrush(Colors.LightGray);
            DrawCircleButton.Foreground = new SolidColorBrush(Colors.Black);

            DrawLinesButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            DrawLinesButton.Foreground = new SolidColorBrush(Colors.White);

            FreeDrawButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            FreeDrawButton.Foreground = new SolidColorBrush(Colors.White);
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            undoLastInk();
        }

        private void ShowTimerButton_Click(object sender, RoutedEventArgs e)
        {
            if (timerText.Visibility == Visibility.Collapsed)
            {
                setTimerVisibility(Visibility.Visible);
            }
            else
            {
                setTimerVisibility(Visibility.Collapsed);
            }
        }

        private void setTimerVisibility(Visibility v, bool resetTimer = false)
        {

            if (v == Visibility.Visible)
            {
                timerText.Visibility = Visibility.Visible;
                ShowTimerButton.Background = new SolidColorBrush(Colors.LightGray);
                ShowTimerButton.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                timerText.Visibility = Visibility.Collapsed;
                ShowTimerButton.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
                ShowTimerButton.Foreground = new SolidColorBrush(Colors.White);

            }

            if (resetTimer)
            {
                numSeconds = 0;
            }
        }

        private void StopWatch_Tick(object sender, object e)
        {
            numSeconds += .1 * mediaPlayer.PlaybackRate;
            if (mediaPlayer.PlaybackRate != 1)
            {
                timerText.Text = numSeconds.ToString("#0.00") + "s";
            }
            else
            {
                timerText.Text = numSeconds.ToString("#0.0") + "s";
            }
        }

        private void timerText_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if (stopWatch.IsEnabled)
            {
                stopWatch.Stop();
            }
            else
            {
                stopWatch.Start();
            }
        }

        private void timerText_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            stopWatch.Stop();
            numSeconds = 0;
            timerText.Text = numSeconds.ToString("#0.0") + "s";
        }

        private void shapeCanvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (numActiveContacts == 2)
            {
                Debug.WriteLine("Manip cumulative x: " + e.Cumulative.Translation.X);
            }
            if (numActiveContacts == 3)
            {
                playbackRateText.Margin = new Thickness(e.Position.X, e.Position.Y, 0, 0);
            }
        }

        private void shapeCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var canvas = (Canvas)sender;
            if (numActiveContacts > 1)
            {
                manipulating = true;
            }

            if (numActiveContacts == 2)
            {
                double regionPercentage = e.Cumulative.Translation.X / canvas.ActualWidth;
                double millisecondsToScrub = mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * regionPercentage * mediaPlayer.PlaybackRate;
                //Debug.WriteLine("Scrubbing (milliseconds): " + millisecondsToScrub);
                mediaPlayer.Position = mediaPlayerScrubStartTime + (TimeSpan.FromMilliseconds(millisecondsToScrub));
            }
            else if (numActiveContacts == 3)
            {
                playbackRateText.Visibility = Visibility.Visible;
                double currentYPos = e.Cumulative.Translation.Y;
                double regionPercentage = (currentYPos - posAtLastPlaybackRateChange) / canvas.ActualHeight;
                if (regionPercentage > .1)
                {
                    if (mediaPlayer.PlaybackRate > .125)
                    {
                        mediaPlayer.PlaybackRate /= 2;
                    }
                    posAtLastPlaybackRateChange = currentYPos;
                }
                else if (regionPercentage < -.1)
                {
                    if (mediaPlayer.PlaybackRate < 4)
                    {
                        mediaPlayer.PlaybackRate *= 2;
                    }
                    posAtLastPlaybackRateChange = currentYPos;
                }
                playbackRateText.Text = mediaPlayer.PlaybackRate.ToString() + "x";
                playbackRateText.Margin = new Thickness(playbackRateText.Margin.Left, e.Position.Y, 0, 0);
            }
        }

        private void shapeCanvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {

            posAtLastPlaybackRateChange = 0;
            playbackRateText.Visibility = Visibility.Collapsed;
        }

        private async void openLatestFile(object sender, TappedRoutedEventArgs e)
        {
            if (currentFolder == null)
            {
                await SetFolder();
            }

            var query = CommonFileQuery.DefaultQuery;
            var queryOptions = new QueryOptions(query, new[] { ".wmv", ".mp4", ".mp3", ".wma", ".mov" });
            queryOptions.FolderDepth = FolderDepth.Shallow;
            var queryResult = currentFolder.CreateFileQueryWithOptions(queryOptions);
            var storageFiles = await queryResult.GetFilesAsync();

            if (storageFiles.Count > 0)
            {

                var file = storageFiles.OrderByDescending(f => f.DateCreated).First();


                if (file != null)
                {
                    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    mediaPlayer.SetSource(stream, file.ContentType);

                    mediaPlayer.Play();
                    mediaPlayer.IsMuted = true;

                }
            }
            else
            {
                currentFolder = null;
            }


        }
    }
}
