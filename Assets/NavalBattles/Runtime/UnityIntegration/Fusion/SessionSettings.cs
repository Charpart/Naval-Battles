using System;

namespace NavalBattles.Runtime.UnityIntegration.Fusion
{
    public sealed class SessionSettings
    {
        public string sessionName { get; }
        public int sceneBuildIndex { get; }

        public SessionSettings(string sessionName, int sceneBuildIndex)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                throw new ArgumentException("Session name must not be empty.", nameof(sessionName));

            if (sceneBuildIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sceneBuildIndex));

            this.sessionName = sessionName;
            this.sceneBuildIndex = sceneBuildIndex;
        }
    }
}
