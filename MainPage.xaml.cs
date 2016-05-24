using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;

namespace LampModule3
{
    public sealed partial class MainPage : Page
    {
        private LampHelper lampHelper;
        private bool lampFound = false;
        Random rand = new Random();
        private bool isLooping = false; //
        private DispatcherTimer dispatchTimer;

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
            if (lampFound)
            {
                /*var slideColor = Color.FromArgb((uint) hueSlider.Value);
                Windows.UI.Color.FromArgb();
                var undividedColor = hueSlider.Value;
                this.Background = Color.FromArgb(undividedColor & 255, )*/
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

        private void button_Click(object sender, RoutedEventArgs e)
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

        private void Grid_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {

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
