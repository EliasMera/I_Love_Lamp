using System;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;

namespace LampModule3
{
    public class VoiceCommands
    {
        private static bool onStateChangeRequested = false;
        private static uint color_value;

        public async static void RegisterVoiceCommands()
        {
            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///LampVoiceCommands.xml"));
            await VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(storageFile); 
        }

        public static void ProcessVoiceCommand(VoiceCommandActivatedEventArgs eventArgs)
        {
            LampHelper lampHelper = new LampHelper();
            switch (eventArgs.Result.RulePath[0])
            {
                case "ToggleLamp":
                    string switchableStateChange = eventArgs.Result.SemanticInterpretation.Properties["switchableStateChange"][0];
                    if (string.Equals(switchableStateChange, "on", StringComparison.OrdinalIgnoreCase))
                    {
                        onStateChangeRequested = true;
                    }
                    else
                    {
                        onStateChangeRequested = false;
                    }
                    
                    lampHelper.LampFound += LampHelper_LampFound;
                    break;
                case "SwitchHue":
                    //do things
                    string color = eventArgs.Result.SemanticInterpretation.Properties["color"][0];
                    lampHelper.LampFound += LampHelper_LampFound_color;
                    switch (color)
                    {
                        case "red":
                            color_value = 110;
                            break;
                        case "blue":
                            color_value = 2495806458;
                            break;
                    }

                    lampHelper.LampFound += LampHelper_LampFound_color;
                    break;
                default:
                    break;
            }
        }

        private static void LampHelper_LampFound(object sender, EventArgs e)
        {
            LampHelper lampHelper = sender as LampHelper;
            lampHelper.SetOnOffAsync(onStateChangeRequested);
        }

        private static void LampHelper_LampFound_color(object sender, EventArgs e)
        {
            LampHelper lampHelper = sender as LampHelper;
            lampHelper.SetHueAsync((uint) color_value);
        }
    }
}
