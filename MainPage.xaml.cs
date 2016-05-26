using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using System.Threading.Tasks;         // Used to implement asynchronous methods
using Windows.Devices.Enumeration;    // Used to enumerate cameras on the device
using Windows.Devices.Sensors;        // Orientation sensor is used to rotate the camera preview
using Windows.Graphics.Display;       // Used to determine the display orientation
using Windows.Graphics.Imaging;       // Used for encoding captured images
using Windows.Media;                  // Provides SystemMediaTransportControls
using Windows.Media.Capture;          // MediaCapture APIs
using Windows.Media.MediaProperties;  // Used for photo and video encoding
using Windows.Storage;                // General file I/O
using Windows.Storage.FileProperties; // Used for image file encoding
using Windows.Storage.Streams;        // General file I/O
using Windows.System.Display;         // Used to keep the screen awake during preview and capture
using Windows.UI.Core;                // Used for updating UI from within async operations
using System.Diagnostics;
using System.Linq;

namespace LampModule3
{
    public sealed partial class MainPage : Page
    {
        private LampHelper lampHelper;
        private bool lampFound = false;
        Random rand = new Random();
        private bool isLooping = false;
        private DispatcherTimer dispatchTimer;
        private DateTime lastTimeChecked;

        // Facial recognition stuff
        FaceDetectionEffect _faceDetectionEffect;
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
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");
        private bool usingFront = true;

        public MainPage()
        {
            this.InitializeComponent();
            hueSlider.Maximum = (double)uint.MaxValue;
            saturationSlider.Maximum = (double)uint.MaxValue;
            brightnessSlider.Maximum = (double)uint.MaxValue;

            lampHelper = new LampHelper();
            lampHelper.LampFound += LampHelper_LampFound;
            lampHelper.LampStateChanged += LampHelper_LampStateChanged;

            // Setting up timer for random colors
            dispatchTimer = new DispatcherTimer();
            dispatchTimer.Interval = new TimeSpan(0, 0, 1);
            dispatchTimer.Tick += setRandomHue;

            // Initialize face detection stuff
            InitializeCameraAsync(true);
        }

        // Initialize facial detection
        private async Task initializeFaceDetection()
        {
            // Create the definition, which will contain some initialization settings
            var definition = new FaceDetectionEffectDefinition();

            // To ensure preview smoothness, do not delay incoming samples
            definition.SynchronousDetectionEnabled = false;

            // In this scenario, choose detection speed over accuracy
            definition.DetectionMode = FaceDetectionMode.HighPerformance;

            // Add the effect to the preview stream
            _faceDetectionEffect = (FaceDetectionEffect)await _mediaCapture.AddVideoEffectAsync(definition, MediaStreamType.VideoPreview);

            // Choose the shortest interval between detection events
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);

            // Start detecting faces
            _faceDetectionEffect.Enabled = true;

            // Register for face detection events
            _faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;

        }

        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                setFaceInfoText(args.ResultFrame.DetectedFaces.Count);
            });            
        }

        private void setFaceInfoText(int faces)
        {
            if (faceToggle.IsOn)
            {
                if (faces > 0)
                {
                    lampHelper.SetSaturationAsync(uint.MaxValue);
                }

                else
                {
                    lampHelper.SetSaturationAsync(0);
                }
            }
            
            faceInfoText.Text = "Faces detected: " + faces.ToString();
        }

        private async void setRandomHue(object sender, object e)
        {
            if (lampFound)
            {
                await lampHelper.SetHueAsync((uint)rand.Next());
            }
        }

        private void LampHelper_LampFound(object sender, EventArgs e)
        {
            lampFound = true;
            GetLampState();
        }

        private void LampHelper_LampStateChanged(object sender, EventArgs e)
        {
            GetLampState();
        }

        private void LampSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (lampFound)
            {
                lampHelper.SetOnOffAsync(((ToggleSwitch)sender).IsOn);
            }
        }

        private async void SetHue_Clicked(object sender, RoutedEventArgs e)
        {
            uint colorInt = (uint)hueSlider.Value;

            // Converts the slider value to a brush from a color (via bitshifting) so we can set the background
            // of the windows form app
            var brush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(byte.MaxValue,
                    (byte)(colorInt >> 16), (byte)(colorInt >> 8), (byte)(colorInt >> 0)));

            if (lampFound)
            {
                await lampHelper.SetHueAsync((uint) hueSlider.Value);
            }
        }

        private void SetSaturation_Clicked(object sender, RoutedEventArgs e)
        {
            if (lampFound)
            {
                lampHelper.SetSaturationAsync((uint)saturationSlider.Value);
            }
        }

        private void SetBrightness_Clicked(object sender, RoutedEventArgs e)
        {
            if (lampFound)
            {
                lampHelper.SetBrightnessAsync((uint)brightnessSlider.Value);
            }
        }

        private async void GetLampState()
        {
            // We don't need to access the lamps to find out how many there are
            deviceStatus.Text = "Devices: " + lampHelper.devicesAttached() as string;
            if (lampFound && (DateTime.Now - lastTimeChecked).TotalMilliseconds > 300)
            {
                try
                {
                    // Get the current On/Off state of the lamp.
                    toggleSwitch.IsOn = await lampHelper.GetOnOffAsync();
                    // Get the current hue, saturation and brightness of the lamp.
                    hueSlider.Value = await lampHelper.GetHueAsync();
                    saturationSlider.Value = await lampHelper.GetSaturationAsync();
                    brightnessSlider.Value = await lampHelper.GetBrightnessAsync();
                    lastTimeChecked = DateTime.Now;
                }
                catch(Exception e)
                {

                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (lampFound)
            {
                isLooping = !isLooping;
                if (dispatchTimer.IsEnabled)
                {
                    dispatchTimer.Stop();
                }
                else
                {
                    dispatchTimer.Start();
                }
            }
        }

        private async void Grid_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            // Get path of file dropped
          
        }

        private void ambientToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            lampHelper.setAdaptiveBrightness(ambientToggleSwitch.IsOn);
        }

        private async void rootLayout_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    //musicFile = storageFile;
                }
            }
        }

        private void rootLayout_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
        }
        
        // Set up video capture device
        private async Task InitializeCameraAsync(bool useFrontPanel)
        {
            var panel = Windows.Devices.Enumeration.Panel.Back;

            if (useFrontPanel)
            {
                panel = Windows.Devices.Enumeration.Panel.Front;
            }

            if (_mediaCapture == null)
            {
                // Get available devices for capturing pictures
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                // Get the desired camera by panel
                DeviceInformation cameraDevice =
                    allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null &&
                    x.EnclosureLocation.Panel == panel);

                // If there is no camera on the specified panel, get any camera
                cameraDevice = cameraDevice ?? allVideoDevices.FirstOrDefault();

                if (cameraDevice == null)
                {
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when video recording has reached the maximum time and when something goes wrong
               // _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

                var mediaInitSettings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(mediaInitSettings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                // If initialization succeeded, start the preview
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _externalCamera = false;
                    }

                    await StartPreviewAsync();
                    await initializeFaceDetection();
                }
            }
        }

        // Convert orientation
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

        //Set up on screen cam preview
        private async Task StartPreviewAsync()
        {
            // Set the preview source in the UI and mirror it if necessary
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start the preview
            try
            {
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when starting the preview: {0}", ex.ToString());
            }

            // Initialize the preview to the current orientation
            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (_externalCamera) return;

            // Populate orientation variables with the current state
            _displayOrientation = _displayInformation.CurrentOrientation;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees((DisplayOrientations)_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);

        }

        private async Task StopPreviewAsync()
        {
            // Stop the preview
            try
            {
                _isPreviewing = false;
                await _mediaCapture.StopPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping the preview: {0}", ex.ToString());
            }

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Cleanup the UI
                PreviewControl.Source = null;
            });
        }

        private async Task CleanupCameraAsync()
        {
            Debug.WriteLine("CleanupCameraAsync");

            if (_isInitialized)
            {
                // If a recording is in progress during cleanup, stop it to save the recording
                if (_isRecording)
                {
                    await StopRecordingAsync();
                }

                if (_isPreviewing)
                {
                    // The call to MediaCapture.Dispose() will automatically stop the preview
                    // but manually stopping the preview is good practice
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            await StopRecordingAsync();
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await CleanupCameraAsync();
        }

        private async Task StopRecordingAsync()
        {
            try
            {
                Debug.WriteLine("Stopping recording...");

                _isRecording = false;
                await _mediaCapture.StopRecordAsync();

                Debug.WriteLine("Stopped recording!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping video recording: {0}", ex.ToString());
            }
        }

        private async void cameraSelectButton_Click(object sender, RoutedEventArgs e)
        {
            await CleanupCameraAsync();
            usingFront = !usingFront;
            await InitializeCameraAsync(usingFront);
            await initializeFaceDetection();
        }
    }

}
