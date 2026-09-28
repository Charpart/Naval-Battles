using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class PlayersStatusView : MonoBehaviour
    {
        [SerializeField] private PlayerStatusView _firstPlayer;
        [SerializeField] private PlayerStatusView _secondPlayer;

        public void Render(PlayerStatusModel[] players)
        {
            _firstPlayer.Render(players[0]);
            _secondPlayer.Render(players[1]);
        }

        public void RenderWaiting(string firstPlayerLabel, string secondPlayerLabel)
        {
            _firstPlayer.RenderWaiting(firstPlayerLabel);
            _secondPlayer.RenderWaiting(secondPlayerLabel);
        }
    }
}
