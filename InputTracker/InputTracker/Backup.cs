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

namespace OpenBVETrainPluginBackup
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
            string documentsFolder = @"F:\OpenBVE_Data";
            Directory.CreateDirectory(documentsFolder); // Ensure the directory exists
            logFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "train_data_log.csv");
            StateFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "state.json");
            ActionFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "action.json");
            ErrorLogFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "plugin_log.txt");
            // Create the directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // Create the log file if it doesn't exist, and write the header
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,SpeedLimit,CurrentSectionLimit,Score\n");
            }
            if(!File.Exists(StateFilePath))
            {
                File.WriteAllText(StateFilePath, JsonConvert.SerializeObject( "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,Score\n", Formatting.Indented));
            }

            properties.AISupport = AISupport.None; // Disable AI support
            return true; // Return true to indicate the plugin has loaded successfully
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
