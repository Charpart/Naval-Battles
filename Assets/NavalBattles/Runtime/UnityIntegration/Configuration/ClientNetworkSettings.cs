using System;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Transport.Simulation;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    public sealed class ClientNetworkSettings
    {
        public static readonly ClientNetworkSettings Default = new ClientNetworkSettings(
            NetworkSimulationSettings.None,
            new ClientRecoverySettings(2.0, 6.0, 1.0));

        public NetworkSimulationSettings simulationSettings { get; }
        public ClientRecoverySettings recoverySettings { get; }

        public ClientNetworkSettings(
            NetworkSimulationSettings simulationSettings,
            ClientRecoverySettings recoverySettings)
        {
            this.simulationSettings = simulationSettings
                ?? throw new ArgumentNullException(nameof(simulationSettings));
            this.recoverySettings = recoverySettings
                ?? throw new ArgumentNullException(nameof(recoverySettings));
        }
    }
}
