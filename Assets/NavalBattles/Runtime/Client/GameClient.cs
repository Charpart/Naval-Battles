using System;
using System.Collections.Generic;
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
        private const int MAX_PENDING_TIME_SAMPLES = 32;

        private readonly IMessageTransport _transport;
        private readonly IClientIdentityStore _identityStore;
        private readonly IProtocolSerializer _serializer;
        private readonly TransportConnectionId _serverConnectionId;
        private readonly ClientRecoveryTimer _recoveryTimer;
        private readonly double _timeSynchronizationIntervalSeconds;
        private readonly double _timeSampleMaxAgeSeconds;
        private ulong _nextMessageId = 1;
        private readonly PendingFireCommand _pendingCommand = new PendingFireCommand();
        private readonly Dictionary<ulong, double> _timeSampleRequests = new Dictionary<ulong, double>();
        private byte[] _sessionPayload;
        private bool _wasSessionAccepted;
        private bool _hasServerClockOffset;
        private double _currentTime;
        private double _lastAppliedTimeSampleRequestTime = double.NegativeInfinity;
        private double _nextTimeSampleTime;
        private double _serverClockOffset;

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
            _timeSynchronizationIntervalSeconds = recoverySettings.heartbeatIntervalSeconds;
            _timeSampleMaxAgeSeconds = recoverySettings.heartbeatTimeoutSeconds;
            _serverConnectionId = serverConnectionId;
            connectionState = ClientConnectionState.Disconnected;
            player = NetworkPlayerSlot.None;
        }

        public void Connect()
        {
            _timeSampleRequests.Clear();
            _nextTimeSampleTime = _currentTime;
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
            AdvanceTime(clientTime);

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
            Receive(payload, _currentTime);
        }

        public void Receive(ReadOnlyMemory<byte> payload, double clientTime)
        {
            AdvanceTime(clientTime);

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
                    ApplyTimeSample(message.messageId, message.serverTime);
                    break;

                case MessageType.RequestRejected:
                    _pendingCommand.TryComplete(
                        message.commandId,
                        RequestStatus.Rejected,
                        message.rejectionReason);
                    break;
            }
        }

        private void AdvanceTime(double clientTime)
        {
            _currentTime = clientTime;
            _recoveryTimer.Advance(clientTime);
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

            if (_hasServerClockOffset)
            {
                localTurnDeadline = snapshot.turnDeadline - _serverClockOffset;
                return;
            }

            double remainingSeconds = Math.Max(0.0, snapshot.turnDeadline - serverTime);
            localTurnDeadline = _currentTime + remainingSeconds;
        }

        private void ApplyTimeSample(ulong messageId, double serverTime)
        {
            if (_timeSampleRequests.Remove(messageId, out double requestTime) == false)
                return;

            double roundTripSeconds = Math.Max(0.0, _currentTime - requestTime);

            if (roundTripSeconds > _timeSampleMaxAgeSeconds ||
                requestTime <= _lastAppliedTimeSampleRequestTime)
            {
                return;
            }

            double localTimeAtServer = requestTime + roundTripSeconds * 0.5;
            _serverClockOffset = serverTime - localTimeAtServer;
            _hasServerClockOffset = true;
            _lastAppliedTimeSampleRequestTime = requestTime;
            SynchronizeTurnDeadline(serverTime);
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
            bool isTimeSampleDue = connectionState == ClientConnectionState.Connected &&
                _currentTime >= _nextTimeSampleTime;

            if (isTimeSampleDue == false &&
                _recoveryTimer.IsHeartbeatDue(connectionState) == false)
            {
                return;
            }

            ClientMessage heartbeat = ClientMessage.CreateHeartbeatRequest(
                _identityStore.clientId,
                NextMessageId());
            RecordTimeSampleRequest(heartbeat.messageId);
            Send(heartbeat);
            _nextTimeSampleTime = _currentTime + _timeSynchronizationIntervalSeconds;
            if (connectionState == ClientConnectionState.Connected && snapshot != null)
            {
                Send(ClientMessage.CreateStateRequest(
                    _identityStore.clientId,
                    NextMessageId(),
                    snapshot.revision));
            }
            _recoveryTimer.RecordHeartbeatSent();
        }

        private void RecordTimeSampleRequest(ulong messageId)
        {
            if (_timeSampleRequests.Count >= MAX_PENDING_TIME_SAMPLES)
            {
                ulong oldestMessageId = ulong.MaxValue;

                foreach (ulong pendingMessageId in _timeSampleRequests.Keys)
                {
                    if (pendingMessageId < oldestMessageId)
                        oldestMessageId = pendingMessageId;
                }

                _timeSampleRequests.Remove(oldestMessageId);
            }

            _timeSampleRequests.Add(messageId, _currentTime);
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
