using System;
using System.IO;
using OpenBveApi.Runtime;
using OpenBveApi.Interface;  // Add this for InterfaceQuickReference

namespace OpenBVETrainPlugin
{
    public class TrainPlugin : IRuntime
    {
        // Path to log the data
        private string logFilePath;
        public bool IsPressedS;
        public bool IsPressedA1;
        private string currentScore;  // String to store the score from InterfaceQuickReference

        // This method initializes the plugin and is called once when the train is loaded
        public bool Load(LoadProperties properties)
        {
            // Set the log file path to the user's Documents folder or a local folder
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            logFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "train_data_log.csv");

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // Create the log file if it doesn't exist, and write the header
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,Score\n");
            }

            // Access the InterfaceQuickReference via Translations.QuickReferences
            if (Translations.QuickReferences != null)
            {
                currentScore = Translations.QuickReferences.Score;  // Read the current score
            }

            // Optionally, log train properties here
            properties.AISupport = AISupport.None; // Disable AI support

            return true; // Return true to indicate the plugin has loaded successfully
        }

        // Called when the simulation starts (IRuntime requirement)
        public void Initialize(InitializationModes mode)
        {
            // Initialize any resources or variables if needed
        }

        // Called each frame to retrieve data
        public void Elapse(ElapseData data)
        {
            // Capture data
            double speed = data.Vehicle.Speed.KilometersPerHour;
            int powerNotch = data.Handles.PowerNotch;
            int brakeNotch = data.Handles.BrakeNotch;
            double totalTime = data.TotalTime.Seconds;
            bool IsPressedS2 = IsPressedS;

            // Update the score if available
            if (Translations.QuickReferences != null)
            {
                currentScore = Translations.QuickReferences.Score;  // Read score into the variable
            }

            // Log data to CSV including the current score
            string logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS2},{IsPressedA1}, ,{currentScore}";
            File.AppendAllText(logFilePath, logLine + Environment.NewLine);
        }

        // Handle signal changes and log signal aspects
        public void SetSignal(SignalData[] signals)
        {
            if (signals.Length > 0)
            {
                int signalAspect = signals[0].Aspect; // The aspect of the current signal section

                // Update the last logged line with the signal aspect
                var allLines = File.ReadAllLines(logFilePath);
                if (allLines.Length > 0)
                {
                    string lastLine = allLines[allLines.Length - 1];
                    allLines[allLines.Length - 1] = lastLine.Replace(", ,", $",{signalAspect},");
                    File.WriteAllLines(logFilePath, allLines);
                }
            }
        }

        public void SetPower(int notch) { }
        public void SetBrake(int notch) { }
        public void SetReverser(int position) { }
        public void KeyDown(VirtualKeys key)
        {
            if (key == VirtualKeys.S)
            {
                IsPressedS = true;
            }
            if (key == VirtualKeys.A1)
            {
                IsPressedA1 = true;
            }
        }
        public void KeyUp(VirtualKeys key)
        {
            if (key == VirtualKeys.S)
            {
                IsPressedS = false;
            }
            if (key == VirtualKeys.A1)
            {
                IsPressedA1 = false;
            }
        }

        public void DoorChange(DoorStates oldState, DoorStates newState) { }
        public void HornBlow(HornTypes type) { }
        public void SetBeacon(BeaconData beacon) { }
        public void SetVehicleSpecs(VehicleSpecs specs) { }
        public void PerformAI(AIData data) { }
        public void Unload() { }
    }
}
