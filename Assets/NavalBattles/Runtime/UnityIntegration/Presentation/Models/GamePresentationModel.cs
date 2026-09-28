using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public sealed class GamePresentationModel
    {
        public TurnTimerModel timer { get; }
        public PlayerStatusModel[] players { get; }
        public BoardPanelModel[] boards { get; }
        public RequestStatus requestStatus { get; internal set; }
        public RejectionReason rejectionReason { get; internal set; }

        public GamePresentationModel(GameDefinition definition)
        {
            definition = definition ?? throw new ArgumentNullException(nameof(definition));
            timer = new TurnTimerModel();
            players = new[]
            {
                new PlayerStatusModel { player = NetworkPlayerSlot.First },
                new PlayerStatusModel { player = NetworkPlayerSlot.Second }
            };
            boards = new[]
            {
                new BoardPanelModel(definition),
                new BoardPanelModel(definition)
            };
        }
    }
}
