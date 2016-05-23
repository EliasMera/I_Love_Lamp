using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;
using Windows.UI;
using Windows.Devices.Sensors;
using Windows.UI.Core;

namespace LampModule3
{
    public sealed partial class MainPage : Page
    {
        private LampHelper lampHelper;
        private bool lampFound = false;
        Random rand = new Random();
        private bool isLooping = false; //
        private DispatcherTimer dispatchTimer;
        private LightSensor lightSensor = LightSensor.GetDefault();

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

            // Setting up light sensor
            ScenarioEnable();
        }

        // Initializing light sensor properties and light sensor field
        private void ScenarioEnable()
        {
            if (lightSensor != null)
            {
                // Establish the report interval (in miliseconds)
                lightSensor.ReportInterval = 300;

                // Setting a handler for when a reading changes past the threshold
                lightSensor.ReadingChanged += new Windows.Foundation.TypedEventHandler<LightSensor, LightSensorReadingChangedEventArgs>(ReadingChanged);
            }
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
                await lampHelper.SetHueAsync(colorInt);
            }
            rootLayout.Background = brush;
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
            if (lampFound)
            {
                // Get the current On/Off state of the lamp.
                toggleSwitch.IsOn = await lampHelper.GetOnOffAsync();

                // Get the current hue, saturation and brightness of the lamp.
                hueSlider.Value = await lampHelper.GetHueAsync();
                saturationSlider.Value = await lampHelper.GetSaturationAsync();
                brightnessSlider.Value = await lampHelper.GetBrightnessAsync();
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            /*isCycling = !isCycling;
            if (isCycling)
            {
                await cycleAsync();
            }*/
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

        // Changes brightness of the lights when a change in the ambient light sensor past the threshold is detected
        async private void ReadingChanged(object sender, LightSensorReadingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (ambientToggleSwitch.IsOn && lampFound)
                {

                    uint LIGHT_CUTOFF = 400;
                    LightSensorReading reading = e.Reading;
                    if (reading.IlluminanceInLux > LIGHT_CUTOFF)
                    {
                        lampHelper.SetBrightnessAsync(0);
                    }
                    else
                    {
                        // Get a ratio and scale it to the light bulb's range
                        double illum_value = ((LIGHT_CUTOFF - reading.IlluminanceInLux) / LIGHT_CUTOFF);
                        uint rounded_value = (uint) Math.Ceiling(illum_value * uint.MaxValue);
                        lampHelper.SetBrightnessAsync(rounded_value);
                    }

                    /* TODO:
                    **Take the light sensor reading
                    * Calculate some value that would compensate for the current
                    * ambient light
                    **Set LIFX brightness value to that brightness
                    */
                }
            });
        }
        }
}

//TODO
/*
 * Blend left
 * Blend right
 * Make color as blend of primary colors
 * Ambient lighting
 * Morse code
 * Machine learning
 * 
 */
