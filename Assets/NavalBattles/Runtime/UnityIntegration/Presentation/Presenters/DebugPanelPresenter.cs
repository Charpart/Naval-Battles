using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Transport.Diagnostics;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Runtime.UnityIntegration.Presentation.Views;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Presenters
{
    public sealed class DebugPanelPresenter : IDisposable
    {
        private readonly IDebugPanelView _view;
        private readonly IGameDebugActions _actions;
        private readonly IProtocolSerializer _serializer = new BinaryProtocolSerializer();
        private bool _isInitialized;
        private bool _isMessageLogVisible;

        public DebugPanelPresenter(IDebugPanelView view, IGameDebugActions actions)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _view.connectionChanged += OnConnectionChanged;
            _view.simulationSettingsChanged += OnSimulationSettingsChanged;
            _view.recreationRequested += OnRecreationRequested;
            _view.restartRequested += OnRestartRequested;
            _view.messageLogVisibilityChanged += OnMessageLogVisibilityChanged;
            _actions.messageLogged += OnMessageLogged;
            _view.SetMessageLogVisible(false);
        }

        public void Dispose()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _isInitialized = false;
            _view.connectionChanged -= OnConnectionChanged;
            _view.simulationSettingsChanged -= OnSimulationSettingsChanged;
            _view.recreationRequested -= OnRecreationRequested;
            _view.restartRequested -= OnRestartRequested;
            _view.messageLogVisibilityChanged -= OnMessageLogVisibilityChanged;
            _actions.messageLogged -= OnMessageLogged;
        }

        private void OnConnectionChanged(bool isConnected)
        {
            _actions.SetConnected(isConnected);
        }

        private void OnSimulationSettingsChanged(NetworkSimulationSettings settings)
        {
            _actions.SetNetworkSimulationSettings(settings);
        }

        private void OnRecreationRequested()
        {
            _actions.RecreateClient();
        }

        private void OnRestartRequested()
        {
            _actions.RestartScene();
        }

        private void OnMessageLogVisibilityChanged(bool isVisible)
        {
            _isMessageLogVisible = isVisible;
            _view.SetMessageLogVisible(isVisible);
        }

        private void OnMessageLogged(TransportLogEntry entry)
        {
            if (_isMessageLogVisible == false)
            {
                return;
            }

            string messageName = GetMessageName(entry);
            _view.AppendMessageLog(
                $"[{entry.time:0.00}] {entry.direction} | "
                + $"{entry.status} | {messageName} | {entry.byteCount} B");
        }

        private string GetMessageName(TransportLogEntry entry)
        {
            if (entry.direction == TransportMessageDirection.Outgoing &&
                _serializer.TryDeserializeClient(entry.payload, out ClientMessage client, out _))
            {
                return client.type.ToString();
            }

            if (entry.direction == TransportMessageDirection.Incoming &&
                _serializer.TryDeserializeServer(entry.payload, out ServerMessage server, out _))
            {
                return server.type.ToString();
            }

            return "Unknown";
        }
    }
}
