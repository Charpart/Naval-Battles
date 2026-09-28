using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class TurnTimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _turnLabel;

        public void Render(TurnTimerModel model)
        {
            _turnLabel.text = GetTurnText(model);
            _turnLabel.text += 
                model.state == TurnTimerState.Running ? 
                    $" <color=#29B8D1>{Math.Ceiling(model.secondsRemaining):0}</color> sec." : 
                    string.Empty;
        }

        public void RenderWaiting()
        {
            _turnLabel.text = "Waiting for players";
        }

        private static string GetTurnText(TurnTimerModel model)
        {
            if (model.state == TurnTimerState.WaitingForPlayers)
                return "Waiting for players";

            if (model.state == TurnTimerState.Finished)
                return "Match finished";

            if (model.usesClientRelativeLabels)
                return model.isLocalTurn ? "Your turn" : "Opponent's turn";

            return model.activePlayer == NetworkPlayerSlot.Second
                ? "Player 2's turn"
                : "Player 1's turn";
        }
    }
}
