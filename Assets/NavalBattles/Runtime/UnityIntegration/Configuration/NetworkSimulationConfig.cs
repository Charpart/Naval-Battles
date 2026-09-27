using NavalBattles.Runtime.Transport.Simulation;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    [CreateAssetMenu(
        menuName = "Naval Battles/Network Simulation",
        fileName = "NetworkSimulationConfig")]
    public sealed class NetworkSimulationConfig : ScriptableObject
    {
        [SerializeField, Min(0.0f)] private float _delaySeconds;
        [SerializeField, Min(0.0f)] private float _jitterSeconds;
        [SerializeField, Range(0.0f, 1.0f)] private float _lossProbability;
        [SerializeField, Range(0.0f, 1.0f)] private float _duplicationProbability;
        public NetworkSimulationSettings CreateRuntimeSettings()
        {
            return new NetworkSimulationSettings(
                _delaySeconds,
                _jitterSeconds,
                _lossProbability,
                _duplicationProbability);
        }
    }
}
