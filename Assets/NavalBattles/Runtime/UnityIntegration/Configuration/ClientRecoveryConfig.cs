using NavalBattles.Runtime.Client.Connection;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    [CreateAssetMenu(
        menuName = "Naval Battles/Client Recovery",
        fileName = "ClientRecoveryConfig")]
    public sealed class ClientRecoveryConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float _heartbeatIntervalSeconds = 2.0f;
        [SerializeField, Min(0.1f)] private float _heartbeatTimeoutSeconds = 6.0f;
        [SerializeField, Min(0.1f)] private float _commandRetryIntervalSeconds = 1.0f;

        public ClientRecoverySettings CreateRuntimeSettings()
        {
            return new ClientRecoverySettings(
                _heartbeatIntervalSeconds,
                _heartbeatTimeoutSeconds,
                _commandRetryIntervalSeconds);
        }
    }
}
