using System;
using NavalBattles.Runtime.Transport.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class ClientDebugControlView : MonoBehaviour
    {
        [SerializeField] private Slider _delaySlider;
        [SerializeField] private Slider _jitterSlider;
        [SerializeField] private Slider _lossSlider;
        [SerializeField] private Slider _duplicateSlider;
        [SerializeField] private TMP_Text _valuesLabel;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _disconnectButton;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _recreateButton;

        public event Action<bool> connectionChanged;
        public event Action<NetworkSimulationSettings> simulationSettingsChanged;
        public event Action recreationRequested;

        private void Awake()
        {
            _applyButton.onClick.AddListener(ApplySettings);
            _disconnectButton.onClick.AddListener(Disconnect);
            _connectButton.onClick.AddListener(Connect);
            _recreateButton.onClick.AddListener(RequestRecreation);
            _delaySlider.onValueChanged.AddListener(UpdateValuesLabel);
            _jitterSlider.onValueChanged.AddListener(UpdateValuesLabel);
            _lossSlider.onValueChanged.AddListener(UpdateValuesLabel);
            _duplicateSlider.onValueChanged.AddListener(UpdateValuesLabel);
            UpdateValuesLabel(0.0f);
        }

        private void OnDestroy()
        {
            _applyButton.onClick.RemoveListener(ApplySettings);
            _disconnectButton.onClick.RemoveListener(Disconnect);
            _connectButton.onClick.RemoveListener(Connect);
            _recreateButton.onClick.RemoveListener(RequestRecreation);
            _delaySlider.onValueChanged.RemoveListener(UpdateValuesLabel);
            _jitterSlider.onValueChanged.RemoveListener(UpdateValuesLabel);
            _lossSlider.onValueChanged.RemoveListener(UpdateValuesLabel);
            _duplicateSlider.onValueChanged.RemoveListener(UpdateValuesLabel);
        }

        private void ApplySettings()
        {
            simulationSettingsChanged?.Invoke(new NetworkSimulationSettings(
                _delaySlider.value,
                _jitterSlider.value,
                _lossSlider.value,
                _duplicateSlider.value));
        }

        private void Disconnect()
        {
            connectionChanged?.Invoke(false);
        }

        private void Connect()
        {
            connectionChanged?.Invoke(true);
        }

        private void RequestRecreation()
        {
            recreationRequested?.Invoke();
        }

        private void UpdateValuesLabel(float ignoredValue)
        {
            _valuesLabel.text =
                $"Delay {_delaySlider.value:0.00}s  "
                + $"Jitter {_jitterSlider.value:0.00}s  "
                + $"Loss {_lossSlider.value:P0}  "
                + $"Duplicate {_duplicateSlider.value:P0}";
        }
    }
}
