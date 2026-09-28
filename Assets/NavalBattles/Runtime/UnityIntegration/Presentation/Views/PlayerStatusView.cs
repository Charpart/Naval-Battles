using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class PlayerStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private GameObject _activeMarker;
        [SerializeField] private GameObject _localMarker;

        public void Render(PlayerStatusModel model)
        {
            _playerLabel.text = model.label;
            _statusLabel.text = GetStatusText(model.status);
            _activeMarker.SetActive(model.status == PlayerGameStatus.ActiveTurn);
            _localMarker.SetActive(model.isLocal);
        }

        public void RenderWaiting(string label)
        {
            _playerLabel.text = label;
            _statusLabel.text = "Waiting";
            _activeMarker.SetActive(false);
            _localMarker.SetActive(false);
        }

        private static string GetStatusText(PlayerGameStatus status)
        {
            return status switch
            {
                PlayerGameStatus.ActiveTurn => "Taking turn",
                PlayerGameStatus.WaitingTurn => "Waiting for turn",
                PlayerGameStatus.Winner => "Winner",
                PlayerGameStatus.Loser => "Loser",
                _ => "Waiting"
            };
        }
    }
}
