using System;
using NavalBattles.Runtime.Client.Commands;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.Client
{
    public sealed class GameClient
    {
        private readonly IMessageTransport _transport;
        private readonly IClientIdentityStore _identityStore;
        private readonly IProtocolSerializer _serializer;
        private readonly TransportConnectionId _serverConnectionId;
        private readonly ClientRecoveryTimer _recoveryTimer;
        private ulong _nextMessageId = 1;
        private readonly PendingFireCommand _pendingCommand = new PendingFireCommand();
        private byte[] _sessionPayload;
        private bool _wasSessionAccepted;
        private double _currentTime;

        public ClientConnectionState connectionState { get; private set; }
        public NetworkPlayerSlot player { get; private set; }
        public GameDefinition definition { get; private set; }
        public PlayerSnapshot snapshot { get; private set; }
        public double localTurnDeadline { get; private set; }

        public bool hasPendingCommand => _pendingCommand.exists;
        public RequestStatus lastRequestStatus => _pendingCommand.lastStatus;
        public RejectionReason lastRejectionReason => _pendingCommand.lastRejectionReason;

        public GameClient(
            IMessageTransport transport,
            IClientIdentityStore identityStore,
            TransportConnectionId serverConnectionId,
            IProtocolSerializer serializer,
            ClientRecoverySettings recoverySettings)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _identityStore = identityStore ?? throw new ArgumentNullException(nameof(identityStore));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _recoveryTimer = new ClientRecoveryTimer(recoverySettings);
            _serverConnectionId = serverConnectionId;
            connectionState = ClientConnectionState.Disconnected;
            player = NetworkPlayerSlot.None;
        }

        public void Connect()
        {
            _recoveryTimer.RecordServerActivity();
            ClientMessage request;

            if (_wasSessionAccepted)
            {
                ulong knownRevision = snapshot?.revision ?? 0;
                request = ClientMessage.CreateResumeRequest(
                    _identityStore.clientId,
                    NextMessageId(),
                    knownRevision);
                connectionState = ClientConnectionState.Reconnecting;
            }
            else
            {
                request = ClientMessage.CreateConnectRequest(_identityStore.clientId, NextMessageId());
                connectionState = ClientConnectionState.Connecting;
            }

            _sessionPayload = _serializer.Serialize(request);
            SendSessionRequest();
        }

        public bool TryFire(int cellIndex)
        {
            if (CanFire() == false)
                return false;

            ulong commandId = _identityStore.ReserveCommandId();
            ClientMessage request = ClientMessage.CreateFireRequest(
                _identityStore.clientId,
                NextMessageId(),
                commandId,
                snapshot.turnId,
                cellIndex);
            _pendingCommand.Begin(commandId, cellIndex, _serializer.Serialize(request));
            SendPendingCommand();

            return true;
        }

        public void Tick(double clientTime)
        {
            _currentTime = clientTime;
            _recoveryTimer.Advance(clientTime);

            if (_recoveryTimer.IsTimedOut(connectionState))
            {
                connectionState = ClientConnectionState.Disconnected;
                return;
            }

            SendHeartbeatIfDue();
            RetrySessionIfDue();
            RetryCommandIfDue();
        }

        public void Receive(ReadOnlyMemory<byte> payload)
        {
            if (_serializer.TryDeserializeServer(payload, out ServerMessage message, out _) == false)
                return;

            _recoveryTimer.RecordServerActivity();
            switch (message.type)
            {
                case MessageType.SessionAccepted:
                    ApplySession(message);
                    break;

                case MessageType.FireResult:
                    ApplyFireResult(message);
                    break;

                case MessageType.StateSnapshot:
                    ApplyIfNewer(message.snapshot, message.serverTime);
                    ClearProcessedPending(message.snapshot);
                    break;

                case MessageType.HeartbeatResponse:
                    SynchronizeTurnDeadline(message.serverTime);
                    break;

                case MessageType.RequestRejected:
                    _pendingCommand.TryComplete(
                        message.commandId,
                        RequestStatus.Rejected,
                        message.rejectionReason);
                    break;
            }
        }

        private bool CanFire()
        {
            return connectionState == ClientConnectionState.Connected
                   && hasPendingCommand == false
                   && snapshot is { winner: NetworkPlayerSlot.None }
                   && snapshot.activePlayer == player;
        }

        private void ApplySession(ServerMessage message)
        {
            player = message.player;
            definition = message.definition;
            ApplyIfNewer(message.snapshot, message.serverTime, true);
            connectionState = ClientConnectionState.Connected;
            _wasSessionAccepted = true;
            _sessionPayload = null;
            ReconcilePendingCommand(snapshot);
        }

        private void ApplyFireResult(ServerMessage message)
        {
            _pendingCommand.TryComplete(
                message.commandId,
                message.requestStatus,
                message.rejectionReason);
            ApplyIfNewer(message.snapshot, message.serverTime);
        }

        private void ApplyIfNewer(
            PlayerSnapshot receivedSnapshot,
            double serverTime,
            bool acceptEqualRevision = false)
        {
            if (receivedSnapshot != null &&
                (snapshot == null || receivedSnapshot.revision > snapshot.revision ||
                    acceptEqualRevision && receivedSnapshot.revision == snapshot.revision))
            {
                snapshot = receivedSnapshot;
                SynchronizeTurnDeadline(serverTime);
            }
        }

        private void SynchronizeTurnDeadline(double serverTime)
        {
            if (snapshot == null)
                return;

            double remainingSeconds = Math.Max(0.0, snapshot.turnDeadline - serverTime);
            localTurnDeadline = _currentTime + remainingSeconds;
        }

        private void ReconcilePendingCommand(PlayerSnapshot receivedSnapshot)
        {
            if (hasPendingCommand == false)
                return;

            if (_pendingCommand.TryReconcile(receivedSnapshot))
                return;

            SendPendingCommand();
        }

        private void ClearProcessedPending(PlayerSnapshot receivedSnapshot)
        {
            _pendingCommand.TryReconcile(receivedSnapshot);
        }

        private void SendHeartbeatIfDue()
        {
            if (_recoveryTimer.IsHeartbeatDue(connectionState) == false)
                return;

            Send(ClientMessage.CreateHeartbeatRequest(_identityStore.clientId, NextMessageId()));
            if (connectionState == ClientConnectionState.Connected && snapshot != null)
            {
                Send(ClientMessage.CreateStateRequest(
                    _identityStore.clientId,
                    NextMessageId(),
                    snapshot.revision));
            }
            _recoveryTimer.RecordHeartbeatSent();
        }

        private void RetrySessionIfDue()
        {
            if (_recoveryTimer.IsSessionRetryDue(connectionState, _sessionPayload != null))
                SendSessionRequest();
        }

        private void SendSessionRequest()
        {
            _transport.Send(_serverConnectionId, _sessionPayload);
            _recoveryTimer.RecordCommandSent();
        }

        private void RetryCommandIfDue()
        {
            if (_recoveryTimer.IsRetryDue(connectionState, hasPendingCommand))
                SendPendingCommand();
        }

        private void SendPendingCommand()
        {
            _transport.Send(_serverConnectionId, _pendingCommand.payload);
            _recoveryTimer.RecordCommandSent();
        }

        private void Send(ClientMessage message)
        {
            _transport.Send(_serverConnectionId, _serializer.Serialize(message));
        }

        private ulong NextMessageId()
        {
            return _nextMessageId++;
        }
    }
}
