using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TrainManager;
using TrainManager.Trains;
using OpenBveApi.Runtime;
using Newtonsoft.Json.Linq;
using System.ComponentModel.Design;

namespace OpenBVETrainPlugin2
{
    // Implements IScoreRuntime (which likely extends IRuntime)
    public class InputTracker : IScoreRuntime
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

        // Plugin load: setup files and establish a persistent TCP connection.
        public bool Load(LoadProperties properties)
        {
            try
            {
                // Set up directories and file paths.
                string documentsFolder = @"E:\OpenBVE_Data";
                Directory.CreateDirectory(documentsFolder);
                logFilePath = Path.Combine(documentsFolder, "OpenBVE_Train_Data", "train_data_log.csv");
                StateFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "state.json");
                ActionFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "action.json");
                ErrorLogFilePath = Path.Combine(documentsFolder, "OpenBVETrainData", "plugin_log.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath));

                if (!File.Exists(logFilePath))
                {
                    File.WriteAllText(logFilePath, "TotalTime,Speed(km/h),PowerNotch,BrakeNotch,AWS,A1Alert,SignalAspect,SpeedLimit,CurrentSectionLimit,Score\n");
                }

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

                // Establish a persistent TCP connection.
                listener = new TcpListener(IPAddress.Loopback, 12345);
                listener.Start();
                Log("TCP listener started on port 12345, waiting for connection...");

                // Accept the connection (blocking for simplicity).
                client = listener.AcceptTcpClient();
                Log("TCP connection established with RL agent.");

                // Start a new thread to listen for incoming actions.
                Thread actionListenerThread = new Thread(new ThreadStart(ListenForActions));
                actionListenerThread.IsBackground = true;
                actionListenerThread.Start();

                // Disable AI support.
                properties.AISupport = AISupport.None;
                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText("plugin_load_errors.txt", $"Error in Load: {ex.Message}\n{ex.StackTrace}\n");
                return false;
            }
        }

        // Basic logging helper method.
        private void Log(string message)
        {
            try
            {
                File.AppendAllText(ErrorLogFilePath, $"{DateTime.Now}: {message}\n");
            }
            catch { }
        }

        // Required method from IRuntime.
        public void Initialize(InitializationModes mode)
        {
            Console.WriteLine($"TrainPlugin initialized in mode: {mode}");
        }

        // Handle score events.
        public void ScoreEvent(int Value, ScoreEventToken TextToken, double Duration)
        {
            currentScore += Value;
            Console.WriteLine($"Score updated: {currentScore}, Event Token: {TextToken}, Duration: {Duration}");
        }

        // The simulation update (tick) method.
        public void Elapse(ElapseData data)
        {
            double speed = data.Vehicle.Speed.KilometersPerHour;
            int powerNotch = data.Handles.PowerNotch;
            int brakeNotch = data.Handles.BrakeNotch;
            
            double totalTime = data.TotalTime.Seconds;
            double currentSectionLimit = 0;
            var playerTrain = TrainManagerBase.PlayerTrain;
            if (playerTrain != null)
            {
                currentSectionLimit = playerTrain.CurrentSectionLimit;
            }

            double reward = CalculateReward(data);
            currentScore += reward;
            File.AppendAllText(logFilePath, $"{reward},");

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

            try
            {
                if (client != null && client.Connected)
                {
                    NetworkStream stream = client.GetStream();
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

            string logLine;
            if (playerTrain != null)
            {
                logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,{playerTrain.CurrentRouteLimit},{playerTrain.CurrentSectionLimit},{currentScore}";
            }
            else
            {
                logLine = $"{totalTime},{speed},{powerNotch},{brakeNotch},{IsPressedS},{IsPressedA1}, ,0,0,{currentScore}";
            }
            File.AppendAllText(logFilePath, logLine + Environment.NewLine);

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
            double baseReward = currentScore / 8000.0;
            double penalty = 0;
            var playerTrain = TrainManagerBase.PlayerTrain;
            if (playerTrain != null)
            {
                penalty = data.Vehicle.Speed.KilometersPerHour > playerTrain.CurrentSectionLimit ? -1.0 : 0.0;
            }
            return baseReward + penalty;
        }

        // Process key down events.
        public void KeyDown(VirtualKeys key)
        {
            if (key == VirtualKeys.S)
                IsPressedS = true;
            if (key == VirtualKeys.A1)
                IsPressedA1 = true;
        }

        // Process key up events.
        public void KeyUp(VirtualKeys key)
        {
            if (key == VirtualKeys.S)
                IsPressedS = false;
            if (key == VirtualKeys.A1)
                IsPressedA1 = false;
        }

        // Methods to change controls based on RL actions.
        public void SetPower(int notch)
        {
            var playerTrain = TrainManagerBase.PlayerTrain;
            if (playerTrain != null && playerTrain.Handles.Power != null)
            {
                playerTrain.Handles.Power.ApplyState(notch, false);
                Log($"Set power notch to {notch}");
            }
        }

        public void SetBrake(int notch)
        {
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

        // Stub methods for engine control.
        public void StartEngine()
        {
            var playerTrain = TrainManagerBase.PlayerTrain;
            if (playerTrain != null && playerTrain.Handles.Brake != null)
            {
                playerTrain.Handles.Reverser.ApplyState(1,false);
            }
            Log("Engine started.");
        }

        public void StopEngine()
        {
             var playerTrain = TrainManagerBase.PlayerTrain;
            if (playerTrain != null && playerTrain.Handles.Brake != null)
            {
                playerTrain.Handles.Reverser.ApplyState(0,false);
            }
            Log("Engine started.");
        }

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

        // Optional: Reset simulation state.
        public void ResetSimulation()
        {
            currentScore = 0;
            var playerTrain = TrainManagerBase.PlayerTrain;
            playerTrain.Jump(0,0);
            Log("Simulation state reset.");
        }

        // Process incoming actions from the RL agent.
        private void ProcessIncomingAction(string actionJson)
        {
            try
            {
                // Split the incoming text by newline to separate multiple JSON objects.
                string[] jsonMessages = actionJson.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string msg in jsonMessages)
                {
                    Log($"Processing JSON message: {msg}");
                 try
                    {
                        JObject jObj = JObject.Parse(msg);
                     if (jObj["action"] != null)
                        {
                            // The token type might be an integer, or something else
                            JToken token = jObj["action"];
                            if (token.Type == JTokenType.Integer)
                         {
                            int commandValue = token.Value<int>();
                            var playerTrain = TrainManagerBase.PlayerTrain;
                            if (playerTrain == null)
                            {
                                Log("Player train is null; cannot process numeric action.");
                                continue;
                            }
                            
                            int currentBrake = playerTrain.Handles.Brake.Driver;
                            int currentPower = playerTrain.Handles.Power.Driver;
                            Log($"actionData is an integer: {commandValue}");
                                // Handle your logic
                             if (commandValue == 0)
                            {
                                Log("Action 0 received: Doing nothing.");
                                // Do nothing.
                            }
                            else if (commandValue == 1)
                            {
                                Log("Action 1: increase power, power is currently:" + currentPower+ " Brake is currrently: " + currentBrake);
                                // Action 1:
                                // - If brake is at max (8), start engine.
                                // - Else if brake is 0, increase power.
                                // - Otherwise, decrease brake.
                                if (currentBrake >= 7)
                                {
                                    Log("Action 1: Brake is at max (7), starting engine and decreasing brake.");
                                    SetBrake(currentBrake - 1);
                                    StartEngine();
                                }
                                else if (currentBrake == 0)
                                {
                                    Log("Action 1: Brake is 0, increasing power.");
                                    SetPower(currentPower + 1);
                                }
                                else
                                {
                                    Log("Action 1: Brake is not 0, decreasing brake.");
                                    SetBrake(currentBrake - 1);
                                }
                            }
                            else if (commandValue == 2)
                            {
                                Log("Action 2: decrease power, power is currently:" + currentPower+ " Brake is currrently: " + currentBrake);
                                // Action 2:
                                // - If brake is at max (8), stop engine.
                                // - Else if power is 0, increase brake.
                                // - Otherwise, decrease power.
                                if (currentBrake == 7)
                                {
                                    Log("Action 2: Brake is at max (8), stopping engine.");
                                    StopEngine();
                                }
                                else if (currentPower == 0)
                                {
                                    Log("Action 2: Power is 0, increasing brake.");
                                    SetBrake(currentBrake + 1);
                                }
                                else
                                {
                                    Log("Action 2: Power is not 0, decreasing power.");
                                    SetPower(currentPower - 1);
                                }
                            }
                            else if (token.Type == JTokenType.String)
                            {
                                string commandStr = token.Value<string>();
                                Log($"actionData is a string: {commandStr}");
                                if (commandStr.Equals("reset", StringComparison.OrdinalIgnoreCase))
                                {
                                    Log("Received reset command.");
                                // Implement reset logic if needed.
                                }
                            }
                            else
                            {
                                Log("actionData is of an unknown type: " + token.Type);
                            }
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing incoming action: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing incoming action: {ex.Message}");
            }
        }

        // Dedicated listener thread for incoming actions.
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
                Thread.Sleep(10); // Brief pause to avoid busy waiting.
            }
        }

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
        
    }
}
