import gymnasium as gym  # Using gymnasium
import numpy as np
import socket
import json
import time
from gymnasium import spaces  # Use gymnasium's spaces
from stable_baselines3 import PPO
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, VecNormalize

# Custom hyperbolic discount function (if needed later)
def hyperbolic_discount(t, alpha=3.0, beta=1.0):
    return 1 / (1 + (t / beta))**alpha

class RailwayEnv(gym.Env):
    """
    A custom Gymnasium environment for the railway simulation.
    Communicates with the C# simulator via a persistent TCP socket.
    """
    metadata = {'render.modes': ['human']}

    def __init__(self, host='localhost', port=12345):
        super(RailwayEnv, self).__init__()

        # Define the discrete action space: 0: accelerate, 1: brake, 2: coast.
        self.action_space = spaces.Discrete(3)

        # We'll allow unbounded observations; normalization is handled later.
        self.observation_space = spaces.Box(low=-np.inf, high=np.inf, shape=(9,), dtype=np.float32)

        self.host = host
        self.port = port
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            self.socket.connect((self.host, self.port))
            print("Connected to simulator at {}:{}".format(self.host, self.port))
        except Exception as e:
            print("Could not connect to simulator:", e)
        self.recv_buffer = b""
        self.state = None

    def send_action(self, action):
        """Send an action to the simulator as a JSON message."""
        try:
            # Convert action to a native int to avoid JSON serialization issues
            action_to_send = int(action)
            message = json.dumps({"action": action_to_send}) + "\n"
            self.socket.sendall(message.encode('ascii'))
        except Exception as e:
            print("Error sending action:", e)

    def receive_state(self):
        """
        Wait for a complete JSON message from the simulator.
        Assumes each message is terminated by a newline character.
        """
        try:
            data = self.socket.recv(4096)
            if not data:
                print("No data received; connection might be closed.")
                return None
            self.recv_buffer += data
            # Look for a newline as a message delimiter.
            if b'\n' in self.recv_buffer:
                lines = self.recv_buffer.split(b'\n')
                msg = lines[0]
                self.recv_buffer = b'\n'.join(lines[1:])
                state = json.loads(msg.decode('ascii'))
                return state
            else:
                return None
        except Exception as e:
            print("Error receiving state:", e)
            return None

    def _process_state(self, raw_state):
        """
        Normalize and process the raw state received from the simulator.
        Converts each value to a float and scales it.
        If a value is the string "Infinity", substitute it with a finite value.
        """
        def to_float(val, default=0):
            try:
                # Check for strings representing infinity.
                if isinstance(val, str) and val.lower() in ["infinity", "inf"]:
                    return 1000.0  # Substitute with a large finite value; adjust as needed.
                return float(val)
            except (TypeError, ValueError):
                return default

        processed = np.array([
            to_float(raw_state.get("speed", 0)) / 100.0,
            to_float(raw_state.get("powerNotch", 0)) / 10.0,
            to_float(raw_state.get("brakeNotch", 0)) / 10.0,
            to_float(raw_state.get("totalTime", 0)) / 100.0,
            to_float(raw_state.get("RouteLimit", 0)) / 100.0,
            to_float(raw_state.get("SectionLimit", 0)) / 100.0,
            1.0 if raw_state.get("AWS", False) in [True, "True", 1, "1"] else 0.0,
            1.0 if raw_state.get("A1Alert", False) in [True, "True", 1, "1"] else 0.0,
            to_float(raw_state.get("score", 0)) / 1000.0
        ], dtype=np.float32)
        processed = np.clip(processed, -10.0, 10.0)
        return processed

    def step(self, action):
        """
        Send the action to the simulator, wait for the next state update,
        and return (observation, reward, terminated, truncated, info).
        """
        self.send_action(action)

        # Wait (with timeout) for the simulator to return an updated state.
        start_time = time.time()
        timeout = 1.0  # seconds
        next_state = None
        while next_state is None and (time.time() - start_time) < timeout:
            next_state = self.receive_state()
        if next_state is None:
            # In case of timeout, return the previous state with a penalty.
            reward = -10.0
            terminated = True
            truncated = False
            return self.state, reward, terminated, truncated, {}

        # Process the raw state into a normalized observation.
        observation = self._process_state(next_state)
        print("Raw obs:", observation)
        obs = np.clip(observation, -10.0, 10.0)
        self.state = obs

        # Retrieve reward and done flag from the raw state.
        reward = next_state.get("reward", 0)
        done = next_state.get("done", False)
        terminated = done  # Using 'done' as the termination flag.
        truncated = False  # No truncation condition is provided.

        return observation, reward, terminated, truncated, {}

    def reset(self, seed=None, options=None):
        # 1) Send a reset command to the C# plugin if desired
        message = json.dumps({"action": "reset"}) + "\n"
        self.socket.sendall(message.encode('ascii'))

        # 2) Optionally wait for a new state from the plugin,
        #    or do a partial 'Jump' call in the plugin so the train is re-centered

        start_time = time.time()
        timeout = 1.0
        new_state = None
        while new_state is None and (time.time() - start_time) < timeout:
            new_state = self.receive_state()

        if new_state is None:
            # fallback if no response
            new_state = {"speed": 0, "powerNotch": 0, "brakeNotch": 7, "score": 0}

        observation = self._process_state(new_state)

        # Return (observation, info) instead of just observation
        info = {}
        return observation, info

    def render(self, mode='human'):
        """Simple text-based render."""
        print("Current state:", self.state)

    def close(self):
        self.socket.close()


if __name__ == "__main__":
    # Function to create the environment and wrap it with Monitor.
    def make_env():
        env = RailwayEnv()
        env = Monitor(env)  # Monitor logs episode rewards and lengths.
        return env

    # Create a vectorized environment.
    env = DummyVecEnv([make_env])
    # Wrap with VecNormalize for automatic normalization of observations and rewards.
    env = VecNormalize(env, norm_obs=True, norm_reward=True)

    # Create the PPO model with a lower learning rate for increased stability.
    model = PPO("MlpPolicy", env, verbose=1, learning_rate=1e-4)

    # Train the agent.
    model.learn(total_timesteps=100000)

    # Save the trained model.
    model.save("railway_ppo_model")

    # Testing loop: Run a simulation loop with the trained model.
    obs, info = env.reset()
    for _ in range(1000):
        action, _states = model.predict(obs)
        obs, reward, terminated, truncated, info = env.step(action)
        print("Obs:", obs, "Reward:", reward)
        if terminated or truncated:
            obs, info = env.reset()

    env.close()
