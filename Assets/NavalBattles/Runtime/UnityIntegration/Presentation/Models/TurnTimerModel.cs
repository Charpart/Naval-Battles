using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public sealed class TurnTimerModel
    {
        public TurnTimerState state { get; internal set; }
        public NetworkPlayerSlot activePlayer { get; internal set; }
        public double secondsRemaining { get; internal set; }
        // When true, the active player is displayed as "you" or "opponent".
        public bool usesClientRelativeLabels { get; internal set; }
        public bool isLocalTurn { get; internal set; }
    }
}
