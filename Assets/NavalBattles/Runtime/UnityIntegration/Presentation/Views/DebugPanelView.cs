using System;
using NavalBattles.Runtime.Transport.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class DebugPanelView : MonoBehaviour, IDebugPanelView
    {
        [SerializeField] private ClientDebugControlView _clientControls;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Toggle _messageLogToggle;
        [SerializeField] private GameObject _messageLogContainer;
        [SerializeField] private Text _messageLogText;

        public event Action<bool> connectionChanged;
        public event Action<NetworkSimulationSettings> simulationSettingsChanged;
        public event Action recreationRequested;
        public event Action restartRequested;
        public event Action<bool> messageLogVisibilityChanged;

        private void Awake()
        {
            Subscribe(_clientControls);
            _restartButton.onClick.AddListener(RequestRestart);
            _messageLogToggle.onValueChanged.AddListener(ChangeMessageLogVisibility);
            _messageLogText.text = string.Empty;
        }

        private void OnDestroy()
        {
            Unsubscribe(_clientControls);
            _restartButton.onClick.RemoveListener(RequestRestart);
            _messageLogToggle.onValueChanged.RemoveListener(ChangeMessageLogVisibility);
        }

        public void SetMessageLogVisible(bool isVisible)
        {
            _messageLogContainer.SetActive(isVisible);
            _messageLogToggle.SetIsOnWithoutNotify(isVisible);
        }

        public void AppendMessageLog(string line)
        {
            const int MaxCharacters = 4000;
            _messageLogText.text += line + Environment.NewLine;

            if (_messageLogText.text.Length > MaxCharacters)
            {
                _messageLogText.text = _messageLogText.text.Substring(
                    _messageLogText.text.Length - MaxCharacters);
            }
        }

        public void SetActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        } 

        private void Subscribe(ClientDebugControlView controls)
        {
            controls.connectionChanged += ForwardConnection;
            controls.simulationSettingsChanged += ForwardSimulationSettings;
            controls.recreationRequested += ForwardRecreation;
        }

        private void Unsubscribe(ClientDebugControlView controls)
        {
            controls.connectionChanged -= ForwardConnection;
            controls.simulationSettingsChanged -= ForwardSimulationSettings;
            controls.recreationRequested -= ForwardRecreation;
        }

        private void ForwardConnection(bool isConnected)
        {
            connectionChanged?.Invoke(isConnected);
        }

        private void ForwardSimulationSettings(NetworkSimulationSettings settings)
        {
            simulationSettingsChanged?.Invoke(settings);
        }

        private void ForwardRecreation()
        {
            recreationRequested?.Invoke();
        }

        private void RequestRestart()
        {
            restartRequested?.Invoke();
        }

        private void ChangeMessageLogVisibility(bool isVisible)
        {
            messageLogVisibilityChanged?.Invoke(isVisible);
        }
    }
}
