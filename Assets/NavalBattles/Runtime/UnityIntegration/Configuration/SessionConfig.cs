using NavalBattles.Runtime.UnityIntegration.Fusion;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    [CreateAssetMenu(menuName = "Naval Battles/Session", fileName = "SessionConfig")]
    public sealed class SessionConfig : ScriptableObject
    {
        [SerializeField] private string _sessionName = "naval-battles";
        [SerializeField, Min(0)] private int _networkSceneBuildIndex;

        public SessionSettings CreateRuntimeSettings()
        {
            return new SessionSettings(_sessionName, _networkSceneBuildIndex);
        }
    }
}
