using System;

namespace NavalBattles.Runtime.Transport.Simulation
{
    public sealed class NetworkSimulationSettings
    {
        public static readonly NetworkSimulationSettings None = new NetworkSimulationSettings(0.0, 0.0, 0.0, 0.0);

        public double delaySeconds { get; }
        public double jitterSeconds { get; }
        public double lossProbability { get; }
        public double duplicationProbability { get; }

        public NetworkSimulationSettings(
            double delaySeconds,
            double jitterSeconds,
            double lossProbability,
            double duplicateProbability)
        {
            this.delaySeconds = Math.Max(0, delaySeconds);
            this.jitterSeconds = Math.Max(0, jitterSeconds);
            this.lossProbability = Math.Max(0, lossProbability);
            this.duplicationProbability = Math.Max(0, duplicateProbability);
        }
    }
}
