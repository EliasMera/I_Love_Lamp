using org.allseen.LSF.LampState;
using System;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;
using Windows.Devices.Sensors;
using Windows.UI.Core;
using Windows.UI;

namespace LampModule3
{
    public class LampHelper
    {
        private AllJoynBusAttachment busAttachment = null;
        private LampStateConsumer consumer = null;
        private string lampDeviceId = "b6afb5592fb6fcb7ab5d589c161168f4";
        private Boolean setAdaptive = false;
        private LightSensor lightSensor = LightSensor.GetDefault();

        public LampHelper()
        {
            // Initialize AllJoyn bus attachment.
            busAttachment = new AllJoynBusAttachment();

            // Initialize LampState watcher.
            LampStateWatcher watcher = new LampStateWatcher(busAttachment);

            // Subscribe to watcher added event - to watch for any producers with LampState interface.
            watcher.Added += Watcher_Added;

            // Start the LampState watcher.
            watcher.Start();
            setUpLightSensor();
        }

        public event EventHandler LampFound;
        public event EventHandler LampStateChanged;

        public async Task<bool> GetOnOffAsync()
        {
            if (consumer != null)
            {
                // Get the current On/Off state of the lamp.
                LampStateGetOnOffResult onOffResult = await consumer.GetOnOffAsync();
                if (onOffResult.Status == AllJoynStatus.Ok)
                {
                    return onOffResult.OnOff;
                }
                else
                {
                    throw new Exception(string.Format("Error getting On/Off state - 0x{0:X}", onOffResult.Status));
                }
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async void SetOnOffAsync(bool value)
        {
            if (consumer != null)
            {
                await consumer.SetOnOffAsync(value);
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async Task<uint> GetHueAsync()
        {
            if (consumer != null)
            {
                // Get the current hue of the lamp.
                LampStateGetHueResult hueResult = await consumer.GetHueAsync();
                if (hueResult.Status == AllJoynStatus.Ok)
                {
                    return hueResult.Hue;
                }
                else
                {
                    throw new Exception(string.Format("Error getting hue - 0x{0:X}", hueResult.Status));
                }
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async Task SetHueAsync(uint value)
        {
            if (consumer != null)
            {
                await consumer.SetHueAsync(value);                
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async Task<uint> GetSaturationAsync()
        {
            if (consumer != null)
            {
                // Get the current saturation of the lamp.
                LampStateGetSaturationResult saturationResult = await consumer.GetSaturationAsync();
                if (saturationResult.Status == AllJoynStatus.Ok)
                {
                    return saturationResult.Saturation;
                }
                else
                {
                    throw new Exception(string.Format("Error getting saturation - 0x{0:X}", saturationResult.Status));
                }
            }
            else
            {
                throw new NullReferenceException("No lamp found");
            }
        }

        public async void SetSaturationAsync(uint value)
        {
            if (consumer != null)
            {
                await consumer.SetSaturationAsync(value);
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async Task<uint> GetBrightnessAsync()
        {
            if (consumer != null)
            {
                // Get the current brightness of the lamp.
                LampStateGetBrightnessResult brightnessResult = await consumer.GetBrightnessAsync();
                if (brightnessResult.Status == AllJoynStatus.Ok)
                {
                    return brightnessResult.Brightness;
                }
                else
                {
                    throw new Exception(string.Format("Error getting brightness - 0x{0:X}", brightnessResult.Status));
                }
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        public async void SetBrightnessAsync(uint value)
        {
            if (consumer != null)
            {
                await consumer.SetBrightnessAsync(value);
            }
            else
            {
                throw new NullReferenceException("No lamp found.");
            }
        }

        private async void Watcher_Added(LampStateWatcher sender, AllJoynServiceInfo args)
        {
            AllJoynAboutDataView aboutData = await AllJoynAboutDataView.GetDataBySessionPortAsync(args.UniqueName, busAttachment, args.SessionPort);

            if (aboutData != null && !string.IsNullOrWhiteSpace(aboutData.DeviceId) && string.Equals(aboutData.DeviceId, lampDeviceId))
            {
                // Join session with the producer of the LampState interface.
                LampStateJoinSessionResult joinSessionResult = await LampStateConsumer.JoinSessionAsync(args, sender);

                if (joinSessionResult.Status == AllJoynStatus.Ok)
                {
                    consumer = joinSessionResult.Consumer;
                    LampFound?.Invoke(this, new EventArgs());
                    consumer.Signals.LampStateChangedReceived += Signals_LampStateChangedReceived;
                }
            }
        }

        private void Signals_LampStateChangedReceived(LampStateSignals sender, LampStateLampStateChangedReceivedEventArgs args)
        {
            LampStateChanged?.Invoke(this, new EventArgs());
        }

        public void setAdaptiveBrightness(Boolean isAdaptive)
        {
            setAdaptive = isAdaptive;
        }

        public void setLightSensorInterval(uint ms)
        {
            lightSensor.ReportInterval = ms;
        }

        // Initializing light sensor properties and light sensor field
        private void setUpLightSensor()
        {
            if (lightSensor != null)
            {
                // Establish the report interval (in miliseconds)
                lightSensor.ReportInterval = 300;

                // Setting a handler for when a reading changes past the threshold
                lightSensor.ReadingChanged += new Windows.Foundation.TypedEventHandler<LightSensor, 
                    LightSensorReadingChangedEventArgs>(ReadingChanged);
            }
        }

        async private void ReadingChanged(LightSensor sender, LightSensorReadingChangedEventArgs args)
        {
            if (setAdaptive && consumer != null)
            {
                uint LIGHT_CUTOFF = 400;
                LightSensorReading reading = args.Reading;
                if (reading.IlluminanceInLux > LIGHT_CUTOFF)
                {
                    await consumer.SetBrightnessAsync(0);
                }
                else
                {
                    // Get a ratio and scale it to the light bulb's range
                    // light is roughly logarithmic from lumens to human perception of brightness
                    //double illum_value = 1 - (Math.Log10(reading.IlluminanceInLux) / 5.0);

                    double illum_value = (LIGHT_CUTOFF - reading.IlluminanceInLux) / LIGHT_CUTOFF;
                    // Round the value to the next highest integer (precision loss is negligible when working
                    // on the order of 4 billion
                    uint rounded_value = (uint)Math.Ceiling(illum_value * uint.MaxValue);
                    await consumer.SetBrightnessAsync(rounded_value);
                }
            }
        }
    }
}
