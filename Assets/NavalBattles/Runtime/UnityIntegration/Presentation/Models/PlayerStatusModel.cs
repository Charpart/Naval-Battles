using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public sealed class PlayerStatusModel
    {
        public NetworkPlayerSlot player { get; internal set; }
        public string label { get; internal set; }
        public PlayerGameStatus status { get; internal set; }
        public bool isLocal { get; internal set; }
    }
}
