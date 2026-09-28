using System;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using NavalBattles.Runtime.UnityIntegration.Presentation.Views;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Presenters
{
    public sealed class ClientGamePresenter : IGamePresenter
    {
        private const int OWN_BOARD_INDEX = 0;
        private const int OPPONENT_BOARD_INDEX = 1;
        private const string LOCAL_PLAYER_LABEL = "You";
        private const string OPPONENT_PLAYER_LABEL = "Opponent";

        private readonly GameClient _client;
        private readonly IGameView _view;
        private GamePresentationModel _model;
        private double _currentTime;
        private bool _isInitialized;

        public ClientGamePresenter(GameClient client, IGameView view)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _view.targetCellSelected += OnTargetCellSelected;
            Refresh(_currentTime);
        }

        public void Refresh(double currentTime)
        {
            _currentTime = currentTime;

            if (_client.definition == null || _client.snapshot == null)
            {
                _view.RenderWaiting(LOCAL_PLAYER_LABEL, OPPONENT_PLAYER_LABEL);
                return;
            }

            EnsureModel();
            FillBoards(_client.snapshot);
            FillPlayers(_client.snapshot);
            FillTimer(_client.snapshot);
            _model.requestStatus = _client.lastRequestStatus;
            _model.rejectionReason = _client.lastRejectionReason;
            _view.Render(_model);
        }

        public void Dispose()
        {
            if (_isInitialized == false)
            {
                return;
            }

            _isInitialized = false;
            _view.targetCellSelected -= OnTargetCellSelected;
        }

        private void EnsureModel()
        {
            if (_model != null)
            {
                return;
            }

            _model = new GamePresentationModel(_client.definition);
            _model.boards[OWN_BOARD_INDEX].title = "Your board";
            _model.boards[OPPONENT_BOARD_INDEX].title = "Opponent's board";
            _model.players[0].player = _client.player;
            _model.players[0].label = LOCAL_PLAYER_LABEL;
            _model.players[1].player = GetOpponent(_client.player);
            _model.players[1].label = OPPONENT_PLAYER_LABEL;
        }

        private void FillBoards(PlayerSnapshot snapshot)
        {
            BoardPanelModel ownBoard = _model.boards[OWN_BOARD_INDEX];
            BoardPanelModel opponentBoard = _model.boards[OPPONENT_BOARD_INDEX];
            ownBoard.owner = _client.player;
            ownBoard.isInteractive = false;
            opponentBoard.owner = GetOpponent(_client.player);
            opponentBoard.isInteractive = CanSelectTarget(snapshot);
            BoardPresentationWriter.FillOwnerBoard(ownBoard, snapshot);
            BoardPresentationWriter.FillOpponentBoard(opponentBoard, snapshot);
        }

        private void FillPlayers(PlayerSnapshot snapshot)
        {
            for (int index = 0; index < _model.players.Length; index++)
            {
                PlayerStatusModel player = _model.players[index];
                player.isLocal = player.player == _client.player;
                player.label = player.isLocal
                    ? LOCAL_PLAYER_LABEL
                    : OPPONENT_PLAYER_LABEL;
                player.status = GetPlayerStatus(snapshot, player.player);
            }
        }

        private void FillTimer(PlayerSnapshot snapshot)
        {
            bool isFinished = snapshot.winner != NetworkPlayerSlot.None;
            _model.timer.state = isFinished ? TurnTimerState.Finished : TurnTimerState.Running;
            _model.timer.activePlayer = snapshot.activePlayer;
            _model.timer.secondsRemaining = Math.Max(0.0, _client.localTurnDeadline - _currentTime);
            _model.timer.usesClientRelativeLabels = true;
            _model.timer.isLocalTurn = snapshot.activePlayer == _client.player;
        }

        private PlayerGameStatus GetPlayerStatus(
            PlayerSnapshot snapshot,
            NetworkPlayerSlot player)
        {
            if (snapshot.winner != NetworkPlayerSlot.None)
            {
                return snapshot.winner == player
                    ? PlayerGameStatus.Winner
                    : PlayerGameStatus.Loser;
            }

            return snapshot.activePlayer == player
                ? PlayerGameStatus.ActiveTurn
                : PlayerGameStatus.WaitingTurn;
        }

        private bool CanSelectTarget(PlayerSnapshot snapshot)
        {
            return _client.connectionState == ClientConnectionState.Connected &&
                _client.hasPendingCommand == false &&
                snapshot.winner == NetworkPlayerSlot.None &&
                snapshot.activePlayer == _client.player;
        }

        private void OnTargetCellSelected(int cellIndex)
        {
            if (_model == null)
            {
                return;
            }

            BoardPanelModel board = _model.boards[OPPONENT_BOARD_INDEX];

            if (board.isInteractive == false ||
                cellIndex < 0 ||
                cellIndex >= board.cells.Length ||
                board.cells[cellIndex] != CellVisualState.Unknown)
            {
                return;
            }

            if (_client.TryFire(cellIndex))
            {
                Refresh(_currentTime);
            }
        }

        private static NetworkPlayerSlot GetOpponent(NetworkPlayerSlot player)
        {
            return player == NetworkPlayerSlot.First
                ? NetworkPlayerSlot.Second
                : NetworkPlayerSlot.First;
        }
    }
}
