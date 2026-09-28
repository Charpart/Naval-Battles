using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class GameView : MonoBehaviour, IGameView
    {
        [SerializeField] private TurnTimerView _timer;
        [SerializeField] private PlayersStatusView _players;
        [SerializeField] private BoardPanelView _firstBoard;
        [SerializeField] private BoardPanelView _secondBoard;
        [SerializeField] private TMP_Text _rejectionLabel;

        public event Action<int> targetCellSelected;

        private void Awake()
        {
            _secondBoard.cellSelected += OnTargetCellSelected;
        }

        private void OnDestroy()
        {
            _secondBoard.cellSelected -= OnTargetCellSelected;
        }

        public void RenderWaiting(string firstPlayerLabel, string secondPlayerLabel)
        {
            _timer.RenderWaiting();
            _players.RenderWaiting(firstPlayerLabel, secondPlayerLabel);
            _firstBoard.SetInteractive(false);
            _secondBoard.SetInteractive(false);
            _rejectionLabel.text = string.Empty;
        }

        public void Render(GamePresentationModel model)
        {
            _timer.Render(model.timer);
            _players.Render(model.players);
            _firstBoard.Render(model.boards[0]);
            _secondBoard.Render(model.boards[1]);
            _rejectionLabel.text = model.requestStatus == RequestStatus.Rejected
                ? GetRejectionText(model.rejectionReason)
                : string.Empty;
        }

        private void OnTargetCellSelected(int cellIndex)
        {
            targetCellSelected?.Invoke(cellIndex);
        }

        private static string GetRejectionText(RejectionReason reason)
        {
            return reason switch
            {
                RejectionReason.WrongTurn => "Shot rejected: not your turn",
                RejectionReason.StaleTurn => "Shot rejected: turn has already changed",
                RejectionReason.TurnExpired => "Shot rejected: time expired",
                RejectionReason.InvalidTarget => "Shot rejected: invalid cell",
                RejectionReason.MatchFinished => "Shot rejected: match finished",
                _ => "Shot rejected by server"
            };
        }
    }
}
