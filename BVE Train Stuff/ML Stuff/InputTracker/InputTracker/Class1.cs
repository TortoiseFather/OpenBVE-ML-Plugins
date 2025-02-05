using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenBveApi.Runtime;
using OpenBveApi.Trains;
using TrainManager.Trains;
using Newtonsoft.Json;
using TrainManager;

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
        private string ErrorLogFilePath = "plugin_log.txt";
        public bool Load(LoadProperties properties)
{
    try
    {
        // Set up directories and file paths
        string documentsFolder = @"D:\OpenBVE_Data";
        Directory.CreateDirectory(documentsFolder); // Ensure the directory exists
        logFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "train_data_log.csv");
        StateFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "state.json");
        ActionFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "action.json");
        ErrorLogFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "plugin_log.txt");

        // Create subdirectories if necessary
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath));

        // Create and initialize log file
        if (!File.Exists(logFilePath))
        {
            File.WriteAllText(logFilePath, "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,SpeedLimit,CurrentSectionLimit,Score\n");
        }

        // Create and initialize state file
        if (!File.Exists(StateFilePath))
        {
            var initialState = new
            {
                TotalTime = 0,
                Speed = 0,
                PowerNotch = 0,
                BrakeNotch = 0,
                AWS = false,
                A1Alert = false,
                SignalAspect = 0,
                SpeedLimit = 0,
                CurrentSectionLimit = 0,
                Score = 0
            };
            File.WriteAllText(StateFilePath, JsonConvert.SerializeObject(initialState, Formatting.Indented));
        }

        // Disable AI support
        properties.AISupport = AISupport.None;
        return true; // Plugin successfully loaded
    }
    catch (Exception ex)
    {
        // Log the exception to a local file
        File.AppendAllText("plugin_load_errors.txt", $"Error in Load: {ex.Message}\n{ex.StackTrace}\n");
        return false; // Indicate the plugin failed to load
    }
}

         private void Log(string message)
        {
            try
        {
            File.AppendAllText(ErrorLogFilePath, $"{DateTime.Now}: {message}\n");
        }
            catch
        {
            // If logging fails, silently ignore
        }
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
            if (playerTrain != null)
            {
                double currentRouteLimit = playerTrain.CurrentRouteLimit;
                double currentSectionLimit = playerTrain.CurrentSectionLimit;

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
            try{
                var state = new
                {
                speed,
                powerNotch,
                brakeNotch,
                totalTime,
                RouteLimit = TrainManagerBase.PlayerTrain?.CurrentRouteLimit ?? 0,
                SectionLimit = TrainManagerBase.PlayerTrain?.CurrentSectionLimit ?? 0,
                AWS = IsPressedS,
                A1Alert = IsPressedA1
                };
            File.WriteAllText(StateFilePath, JsonConvert.SerializeObject(state, Formatting.Indented));
            Log("State written to JSON.");
            }
            catch(Exception ex)
            {
                Log($"Error writing state JSON: {ex.Message}");
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
