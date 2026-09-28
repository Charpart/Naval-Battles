using System;
using NavalBattles.Runtime.Transport.Simulation;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public interface IDebugPanelView
    {
        event Action<bool> connectionChanged;
        event Action<NetworkSimulationSettings> simulationSettingsChanged;
        event Action recreationRequested;
        event Action restartRequested;
        event Action<bool> messageLogVisibilityChanged;

        void SetMessageLogVisible(bool isVisible);
        void AppendMessageLog(string line);
    }
}
