import math
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt


def calculate_score(num_stations, N_values, R_Arrival=100, R_OnTime=10, R_PerfectStop=15, B_OnTime=0.10, B_PerfectStop=0.15):
    S = 0
    for i in range(num_stations):
        N = N_values[i]
        S = ((S + R_Arrival + (3 * N) + R_OnTime) * (1 + B_OnTime))
        # Add Perfect Stop reward and apply Perfect Stop bonus
        S = (S + R_PerfectStop) * (1 + B_PerfectStop)
    return S

# Parameters
num_stations_range = [2,3,4,5,6,7,8,9,10,11]  # Range of station numbers for testing
total_signals_range = [4,8,16,24,32,40,50,60,70,80,90,100,110]  # Range of total signal counts for testing
num_seeds = 5  # Number of random seeds per scenario
min_signals_per_station = 1
max_signals_per_station = 10

# Collect results
delta_values = []
N_total_labels = []

for num_stations in num_stations_range:
    for total_signals in total_signals_range:
        # Skip scenarios where the number of stations is greater than or equal to total signals
        if num_stations >= total_signals:
            continue

        avg_signals_per_station = total_signals // num_stations
        N_total = num_stations + total_signals  # Combined N value

        # Ensure avg_signals_per_station is within [1, 10] range
        avg_signals_per_station = min(max(avg_signals_per_station, min_signals_per_station), max_signals_per_station)

        # Test 1: Even Distribution (Clipped to [1, 10] range)
        N_values_even = [avg_signals_per_station] * num_stations
        S_even = calculate_score(num_stations, N_values_even)
        print("N = ", N_total, " Even score: ", S_even)
        # Loop over multiple random seeds to create different random distributions
        for seed in range(num_seeds):
            np.random.seed(seed)

            # Test 2: Random Distribution within [1, 10] range
            N_values_random = np.clip(np.random.poisson(avg_signals_per_station, num_stations), min_signals_per_station,
                                      max_signals_per_station)
            S_random = calculate_score(num_stations, N_values_random)

            # Test 3: Front-Loaded Distribution within [1, 10] range
            N_values_front_loaded = [max_signals_per_station] * min(20, num_stations) + [avg_signals_per_station] * (
                        num_stations - 20)
            S_front_loaded = calculate_score(num_stations, N_values_front_loaded)

            # Test 4: Back-Loaded Distribution within [1, 10] range
            N_values_back_loaded = [avg_signals_per_station] * (num_stations - 20) + [max_signals_per_station] * min(20,
                                                                                                                     num_stations)
            S_back_loaded = calculate_score(num_stations, N_values_back_loaded)

            # Calculate distribution coefficients for each scenario
            alpha_random = np.log(S_random / S_even)
            alpha_front = np.log(S_front_loaded / S_even)
            alpha_back = np.log(S_even / S_back_loaded)

            # Append results grouped by combined N_total
            delta_values.extend([alpha_random, alpha_front, alpha_back])
            N_total_labels.extend([N_total] * 3)

            #print specific values



# Plot the distribution coefficients with combined N values on the x-axis
plt.figure(figsize=(12, 6))
plt.scatter(N_total_labels, delta_values, alpha=0.6, color="blue")
plt.xlabel("Combined Stations + Signals (N total)")
plt.ylabel("Distribution Coefficient (δ)")
plt.title("Distribution Coefficients by Combined N Value")
plt.grid(False)
plt.tight_layout()
plt.savefig("output.png")