using System;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Runtime.Transport.Diagnostics;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Presenters
{
    public interface IGameDebugActions
    {
        event Action<TransportLogEntry> messageLogged;

        void SetConnected(bool isConnected);
        void SetNetworkSimulationSettings(NetworkSimulationSettings settings);
        void RecreateClient();
        void RestartScene();
    }
}
