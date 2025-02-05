using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;
using OpenBveApi.Runtime;
using OpenBveApi.Trains;
using TrainManager.Trains;
using Newtonsoft.Json;
using System.Threading; 
using TrainManager;

namespace OpenBVETrainPlugin2
{
    // Implementing IScoreRuntime, which likely extends IRuntime
    public class backup : IScoreRuntime
    {
        private string logFilePath;
        public bool IsPressedS;
        public bool IsPressedA1;
        private double currentScore = 0;
        private string StateFilePath = "state.json";
        private string ActionFilePath = "action.json";
        private string ErrorLogFilePath = "plugin_log.txt";
        private TcpClient client;
        private TcpListener listener;

        // Plugin load: setup files and a persistent TCP connection.
        public bool Load(LoadProperties properties)
        {
            try
            {
                // Set up directories and file paths
                string documentsFolder = @"E:\OpenBVE_Data";
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

                // Establish a persistent TCP connection
            listener = new TcpListener(IPAddress.Loopback, 12345);
        listener.Start();
        Log("TCP listener started on port 12345, waiting for connection...");

        // Accept the connection (blocking for simplicity)
        client = listener.AcceptTcpClient();
        Log("TCP connection established with RL agent.");

        // Start a new thread to listen for incoming actions.
        Thread actionListenerThread = new Thread(new ThreadStart(ListenForActions));
        actionListenerThread.IsBackground = true;  // So it doesn't block shutdown
        actionListenerThread.Start();

        // Disable AI support and continue initialization.
        properties.AISupport = AISupport.None;
            return true;
            }
            catch (Exception ex)
            {
                // Log the exception to a local file
                File.AppendAllText("plugin_load_errors.txt", $"Error in Load: {ex.Message}\n{ex.StackTrace}\n");
                return false; // Indicate the plugin failed to load
            }
        }

        // Basic logging helper method.
        private void Log(string message)
        {
            try
            {
                File.AppendAllText(ErrorLogFilePath, $"{DateTime.Now}: {message}\n");
            }
            catch
            {
                // Silently ignore logging errors.
            }
        }

        // Required method from IRuntime.
        public void Initialize(InitializationModes mode)
        {
            Console.WriteLine($"TrainPlugin initialized in mode: {mode}");
        }

        // Handle score events here.
        public void ScoreEvent(int Value, ScoreEventToken TextToken, double Duration)
        {
            currentScore += Value; // Update the current score based on the value.
            Console.WriteLine($"Score updated: {currentScore}, Event Token: {TextToken}, Duration: {Duration}");
        }

        // The simulation update (tick) method.
        public void Elapse(ElapseData data)
        {
            // Capture simulation data.
            double speed = data.Vehicle.Speed.KilometersPerHour;
            int powerNotch = data.Handles.PowerNotch;
            int brakeNotch = data.Handles.BrakeNotch;
            double currentSectionLimit = 0;
            var playerTrain = TrainManagerBase.PlayerTrain;  // Accessing the PlayerTrain field in TrainManagerBase
            if (playerTrain != null)
            {
                double currentRouteLimit = playerTrain.CurrentRouteLimit;
                currentSectionLimit = playerTrain.CurrentSectionLimit;
            }

            double totalTime = data.TotalTime.Seconds;

            // Calculate reward. (Note: you might want to compute a delta reward rather than using cumulative currentScore.)
            double reward = CalculateReward(data);
            currentScore += reward; // Update cumulative score.
            File.AppendAllText(logFilePath, $"{reward},");

            // Build the state object BEFORE sending it.
            var state = new
            {
                speed = speed,
                powerNotch = powerNotch,
                brakeNotch = brakeNotch,
                totalTime = totalTime,
                RouteLimit = playerTrain?.CurrentRouteLimit ?? 0,
                SectionLimit = playerTrain?.CurrentSectionLimit ?? 0,
                AWS = IsPressedS,
                A1Alert = IsPressedA1,
                score = currentScore
            };

            // Send state over a persistent socket.
            try
            {
                if (client != null && client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    // Append a newline so the Python side can easily delimit messages.
                    string stateJson = JsonConvert.SerializeObject(state) + "\n";
                    byte[] stateBytes = Encoding.ASCII.GetBytes(stateJson);
                    stream.Write(stateBytes, 0, stateBytes.Length);
                }
                else
                {
                    Log("TCP client not connected.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error sending state over socket: {ex.Message}");
            }

            // Log detailed CSV data.
            string logLine;
            if (playerTrain != null)
            {
                double currentRouteLimit = playerTrain.CurrentRouteLimit;
                double currentSectionLimitLog = playerTrain.CurrentSectionLimit;
                logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,{currentRouteLimit},{currentSectionLimitLog},{currentScore}";
            }
            else
            {
                logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,0,0,{currentScore}";
            }
            File.AppendAllText(logFilePath, logLine + Environment.NewLine);

            // Write state to a JSON file for redundancy/diagnostics.
            try
            {
                File.WriteAllText(StateFilePath, JsonConvert.SerializeObject(state, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log($"Error writing state JSON: {ex.Message}");
            }
        }

        // A simple reward calculation method.
        private double CalculateReward(ElapseData data)
        {
            double baseReward = currentScore / 8000.0; // Normalize using buffer.
            double penalty = 0;
            var playerTrain = TrainManagerBase.PlayerTrain; 
            if (playerTrain!=null){
                penalty = data.Vehicle.Speed.KilometersPerHour > playerTrain.CurrentSectionLimit ? -1.0 : 0.0;
            }
            
            return baseReward + penalty;
        }

        // Update signal information (e.g., for logging).
        public void SetSignal(SignalData[] signals)
        {
            if (signals.Length > 0)
            {
                int signalAspect = signals[0].Aspect; // The aspect of the current signal section.
                // Update the last logged line with the signal aspect.
                var allLines = File.ReadAllLines(logFilePath);
                if (allLines.Length > 0)
                {
                    string lastLine = allLines[allLines.Length - 1];
                    allLines[allLines.Length - 1] = lastLine.Replace(", ,", $",{signalAspect},");
                    File.WriteAllLines(logFilePath, allLines);
                }
            }
        }

        // Process key down events.
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

        // Process key up events.
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

        // The following methods are stubs required by the interface.
        public void SetPower(int notch) {
            var playerTrain = TrainManagerBase.PlayerTrain;
    if (playerTrain != null && playerTrain.Handles.Power != null)
    {
        // Here, 'false' indicates we are setting an absolute value rather than a relative change.
        playerTrain.Handles.Power.ApplyState(notch, false);
        Log($"Set power notch to {notch}");
    }
         }
        public void SetBrake(int notch) {
          var playerTrain = TrainManagerBase.PlayerTrain;
    if (playerTrain != null && playerTrain.Handles.Brake != null)
    {
        playerTrain.Handles.Brake.ApplyState(notch, false);
        Log($"Set brake notch to {notch}");
    }
        }
        public void SetReverser(int position) { }
        public void DoorChange(DoorStates oldState, DoorStates newState) { }
        public void HornBlow(HornTypes type) { }
        public void SetBeacon(BeaconData beacon) { }
        public void SetVehicleSpecs(VehicleSpecs specs) { }
        public void PerformAI(AIData data) { }

        // Unload method: close the persistent TCP connection.
        public void Unload()
    {
        if (client != null)
        {
            client.Close();
            client = null;
        }
        if (listener != null)
        {
            listener.Stop();
            listener = null;
        }
    }

        // Optional: Reset simulation state (to be triggered by the RL environment as needed).
        public void ResetSimulation()
        {
            currentScore = 0;
            // Reset other simulation parameters if needed.
            Log("Simulation state reset.");
        }

        // Optional: Process incoming actions from the RL agent.
        // This is a stub—you might run a dedicated listener thread for incoming messages.
        private void ProcessIncomingAction(string actionJson)
{
    try
    {
        // Split the incoming text by newline (assuming messages are terminated with a newline)
        string[] jsonMessages = actionJson.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string msg in jsonMessages)
        {
            Log($"Processing JSON message: {msg}");
            Log("Action data is: " + msg);
            dynamic actionData = JsonConvert.DeserializeObject(msg);
            Log("Deserialized object: " + actionData?.ToString());
            // Process the action depending on its type.
            // For example, if the JSON is like { "action": "reset" }
            if (actionData.action != null)
            {
                // If the action is a string command.
                if (actionData.action is string)
                {
                    string command = actionData.action;
                    if (command.Equals("reset", StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Received reset command.");
                        // Implement your reset behavior here.
                    }
                    if (actionData.action is string commandStr)
                {
                    Log($"Command received as string: {commandStr}");
                    // Handle your string-based command (e.g. "reset").
                }
                // Check if 'action' is a numeric type
                else if (actionData.action is long || actionData.action is int)
                {
                    int commandVal = (int)actionData.action;
                    Log($"Command received as integer: {commandVal}");
                    // Handle your numeric-based action.
                }
                }
                // If the action is a numeric command.
                else if (actionData.action is long || actionData.action is int)
                {
                    int commandValue = (int)actionData.action;
                    // For example, you might decide that a numeric command means:
                    // - If value is 0, set power notch to 0.
                    // - If value is 1 or 2, it could mean different control commands.
                    Log("Received numeric action: " + commandValue);
                    
                    // For illustration, suppose we interpret numeric actions as follows:
                    // 0 means "do nothing",
                    // 1 means "increase power",
                    // 2 means "decrease brake" (or vice versa, depending on your design).
                    // You would call your control methods accordingly.
                    // Example:
                    if (commandValue == 1)
                    {
                        // Increase power (or set a specific notch)
                        SetPower(3);  // example value
                    }
                    else if (commandValue == 2)
                    {
                        // Decrease brake (or set a specific notch)
                        SetBrake(0);  // example value
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log($"Error processing incoming action: {ex.Message}");
    }
}



// Inside your InputTracker class:
        private void ListenForActions()
        {
        Log("Listener started!");
         while (true)
        {
        try
        {
            if (client != null && client.Available > 0)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string actionJson = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    ProcessIncomingAction(actionJson);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error reading actions: {ex.Message}");
        }
        Thread.Sleep(10); // Brief pause to avoid hogging the CPU.
        }
    }   

    }
    
}
