using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Server;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using NavalBattles.Runtime.UnityIntegration.Presentation.Views;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Presenters
{
    public sealed class ServerGamePresenter : IGamePresenter
    {
        private const int FIRST_BOARD_INDEX = 0;
        private const int SECOND_BOARD_INDEX = 1;

        private readonly GameServer _server;
        private readonly IGameView _view;
        private readonly GamePresentationModel _model;
        private bool _isInitialized;

        public ServerGamePresenter(GameServer server, IGameView view)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = new GamePresentationModel(server.definition);
            _model.players[0].label = "Player 1";
            _model.players[1].label = "Player 2";
            ConfigureBoards();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            Refresh(0.0);
        }

        public void Refresh(double currentTime)
        {
            PlayerSnapshot first = _server.GetPlayerSnapshot(NetworkPlayerSlot.First);
            PlayerSnapshot second = _server.GetPlayerSnapshot(NetworkPlayerSlot.Second);
            BoardPresentationWriter.FillOwnerBoard(_model.boards[FIRST_BOARD_INDEX], first);
            BoardPresentationWriter.FillOwnerBoard(_model.boards[SECOND_BOARD_INDEX], second);
            FillStatus(first, currentTime);
            _view.Render(_model);
        }

        public void Dispose()
        {
            _isInitialized = false;
        }

        private void ConfigureBoards()
        {
            BoardPanelModel first = _model.boards[FIRST_BOARD_INDEX];
            first.owner = NetworkPlayerSlot.First;
            first.title = "Player 1's board";
            first.isInteractive = false;
            BoardPanelModel second = _model.boards[SECOND_BOARD_INDEX];
            second.owner = NetworkPlayerSlot.Second;
            second.title = "Player 2's board";
            second.isInteractive = false;
        }

        private void FillStatus(PlayerSnapshot snapshot, double currentTime)
        {
            bool hasFirst = _server.HasPlayer(NetworkPlayerSlot.First);
            bool hasSecond = _server.HasPlayer(NetworkPlayerSlot.Second);

            if (hasFirst == false || hasSecond == false)
            {
                FillWaitingStatus();
                return;
            }

            FillRunningStatus(snapshot);
            bool isFinished = snapshot.winner != NetworkPlayerSlot.None;
            _model.timer.state = isFinished ? TurnTimerState.Finished : TurnTimerState.Running;
            _model.timer.activePlayer = snapshot.activePlayer;
            _model.timer.secondsRemaining = isFinished
                ? 0.0
                : Math.Max(0.0, snapshot.turnDeadline - currentTime);
            _model.timer.usesClientRelativeLabels = false;
            _model.timer.isLocalTurn = false;
        }

        private void FillWaitingStatus()
        {
            _model.players[0].status = PlayerGameStatus.Waiting;
            _model.players[1].status = PlayerGameStatus.Waiting;
            _model.players[0].isLocal = false;
            _model.players[1].isLocal = false;
            _model.timer.state = TurnTimerState.WaitingForPlayers;
            _model.timer.activePlayer = NetworkPlayerSlot.None;
            _model.timer.secondsRemaining = 0.0;
            _model.timer.usesClientRelativeLabels = false;
            _model.timer.isLocalTurn = false;
        }

        private void FillRunningStatus(PlayerSnapshot snapshot)
        {
            for (int index = 0; index < _model.players.Length; index++)
            {
                PlayerStatusModel player = _model.players[index];
                player.isLocal = false;

                if (snapshot.winner != NetworkPlayerSlot.None)
                {
                    player.status = snapshot.winner == player.player
                        ? PlayerGameStatus.Winner
                        : PlayerGameStatus.Loser;
                }
                else
                {
                    player.status = snapshot.activePlayer == player.player
                        ? PlayerGameStatus.ActiveTurn
                        : PlayerGameStatus.WaitingTurn;
                }
            }
        }
    }
}
