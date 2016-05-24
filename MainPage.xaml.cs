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
        private bool isLooping = false;
        private DispatcherTimer dispatchTimer;
        private DateTime lastTimeChecked;

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
<<<<<<< HEAD
                /*var slideColor = Color.FromArgb((uint) hueSlider.Value);
                Windows.UI.Color.FromArgb();
                var undividedColor = hueSlider.Value;
                this.Background = Color.FromArgb(undividedColor & 255, )*/
                await lampHelper.SetHueAsync((uint) hueSlider.Value);
=======
                await lampHelper.SetHueAsync(colorInt);
>>>>>>> 2b4090e4b2fae19ab3cf0dc165573dc4351feec7
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
            if (lampFound && (DateTime.Now - lastTimeChecked).TotalMilliseconds > 200)
            {
                // Get the current On/Off state of the lamp.
                toggleSwitch.IsOn = await lampHelper.GetOnOffAsync();

                // Get the current hue, saturation and brightness of the lamp.
                hueSlider.Value = await lampHelper.GetHueAsync();
                saturationSlider.Value = await lampHelper.GetSaturationAsync();
                brightnessSlider.Value = await lampHelper.GetBrightnessAsync();
                lastTimeChecked = DateTime.Now;
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

<<<<<<< HEAD
        private void Grid_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {

=======
        private void ambientToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            lampHelper.setAdaptiveBrightness(ambientToggleSwitch.IsOn);
>>>>>>> 2b4090e4b2fae19ab3cf0dc165573dc4351feec7
        }
    }
}
