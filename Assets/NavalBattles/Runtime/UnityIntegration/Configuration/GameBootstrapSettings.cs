using System;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.UnityIntegration.Fusion;

namespace NavalBattles.Runtime.UnityIntegration.Configuration
{
    public sealed class GameBootstrapSettings
    {
        public GameRules rules { get; }
        public int seed { get; }
        public SessionSettings session { get; }
        public ClientNetworkSettings clientNetwork { get; }

        public GameBootstrapSettings(
            GameRules rules,
            int seed,
            SessionSettings session,
            ClientNetworkSettings clientNetwork)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            this.seed = seed;
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.clientNetwork = clientNetwork
                ?? throw new ArgumentNullException(nameof(clientNetwork));
        }
    }
}
