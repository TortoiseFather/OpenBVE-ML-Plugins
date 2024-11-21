using System;
 using System.Collections.Generic;
 using System.IO;
 using System.Linq;
 using System.Threading;
 using System.Threading.Tasks;
using OpenBveApi.Runtime;
using OpenBveApi.Trains;
using TrainManager.Trains;
using TrainManager;
using Newtonsoft.Json;
using Microsoft.CSharp;

namespace OpenBVETrainPlugin
{
    // Implementing IScoreRuntime, which likely extends IRuntime
    public class TrainPlugin : IScoreRuntime 
    {
        private string logFilePath;
        public bool IsPressedS;
        public bool IsPressedA1;
        private int currentScore = 0;

        private string StateFilePath = "state.json";
        private string ActionFilePath = "action.json";

        public bool Load(LoadProperties properties)
        {
            // Set the log file path to the user's Documents folder or a local folder
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            logFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "train_data_log.csv");
            StateFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "state.json");
            ActionFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "action.json");


            // Create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // Create the log file if it doesn't exist, and write the header
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,Score\n");
            }

            properties.AISupport = AISupport.None; // Disable AI support
            return true; // Return true to indicate the plugin has loaded successfully
        }

        // Required method from IRuntime
        public void Initialize(InitializationModes mode)
        {
            // Perform any initialization required by the plugin
            Console.WriteLine($"TrainPlugin initialized in mode: {mode}");
        }
        

        // Handle score events here
        public void ScoreEvent(int Value, ScoreEventToken TextToken, double Duration)
        {
            // Update the current score based on the value
            currentScore += Value; // Assuming this adds to the score

            // Optionally, log or process the score event
            Console.WriteLine($"Score updated: {currentScore}, Event Token: {TextToken}, Duration: {Duration}");
        }

        public void Elapse(ElapseData data)
        {
            // Capture data
            double speed = data.Vehicle.Speed.KilometersPerHour;
            int powerNotch = data.Handles.PowerNotch;
            int brakeNotch = data.Handles.BrakeNotch;
            double totalTime = data.TotalTime.Seconds;
            var playerTrain = TrainManagerBase.PlayerTrain;  // Accessing the PlayerTrain field in TrainManagerBase
            double currentRouteLimit = 0;
            double currentSectionLimit = 0;
            
            if (playerTrain != null)
            {
                currentRouteLimit = playerTrain.CurrentRouteLimit;
                currentSectionLimit = playerTrain.CurrentSectionLimit;

                // Log data to CSV including the current score, CurrentRouteLimit, and CurrentSectionLimit
                string logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,{currentRouteLimit},{currentSectionLimit},{currentScore}";
                File.AppendAllText(logFilePath, logLine + Environment.NewLine);
            }
            else
            {
                // If PlayerTrain is null, log default values
                string logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,0,0,{currentScore}";
                File.AppendAllText(logFilePath, logLine + Environment.NewLine);
            }
            var state = new
            {
                speed,
                powerNotch,
                brakeNotch,
                totalTime,
                currentRouteLimit,
                currentSectionLimit,
                AWS = IsPressedS,
                A1Alert = IsPressedA1
            };

            File.WriteAllText(StateFilePath, JsonConvert.SerializeObject(state, Formatting.Indented));

            // Read the action from the action.json file (if it exists)
             if (File.Exists(ActionFilePath))
                {
                    try
                {
                    string actionJson = File.ReadAllText(ActionFilePath);
                    var action = JsonConvert.DeserializeObject<dynamic>(actionJson);

                // Apply the action (power/brake levels)
                    int power = action.PowerNotch;
                    int brake = action.BrakeNotch;

                    SetPower(power);
                    SetBrake(brake);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading action file: {ex.Message}");
            }
        }
        }

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

        public void SetPower(int notch) { }
        public void SetBrake(int notch) { }
        public void SetReverser(int position) { }
        public void DoorChange(DoorStates oldState, DoorStates newState) { }
        public void HornBlow(HornTypes type) { }
        public void SetBeacon(BeaconData beacon) { }
        public void SetVehicleSpecs(VehicleSpecs specs) { }
        public void PerformAI(AIData data) { }
        public void Unload() { }
    }
}
