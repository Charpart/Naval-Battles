using System;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Server.Connections;
using NavalBattles.Runtime.Server.Snapshots;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.Server
{
    public sealed class GameServer
    {
        private readonly IProtocolSerializer _serializer;
        private readonly ServerResponseSender _responses;
        private readonly MatchState _match;
        private readonly GameDefinition _definition;
        private readonly PlayerSessionRegistry _sessionRegistry = new PlayerSessionRegistry();

        public ulong stateRevision => _match.revision;
        public int connectedPlayerCount => _sessionRegistry.count;

        public GameServer(
            GameRules rules,
            int seed,
            double serverTime,
            IMessageTransport transport,
            IProtocolSerializer serializer)
        {
            transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _responses = new ServerResponseSender(transport, serializer);

            if (MatchSystem.TryCreate(rules, seed, serverTime, out _match) == false)
                throw new ArgumentException("The game rules cannot produce a match.", nameof(rules));

            _definition = CreateDefinition(rules);
        }
        
        private GameDefinition CreateDefinition(GameRules rules)
        {
            int[] shipLengths = new int[rules.shipLengths.Count];
            for (int index = 0; index < shipLengths.Length; index++)
                shipLengths[index] = rules.shipLengths[index];

            return new GameDefinition(rules.width, rules.height, shipLengths, rules.turnDurationSeconds);
        }

        public void Receive(TransportConnectionId connectionId, ReadOnlyMemory<byte> payload, double serverTime)
        {
            if (_serializer.TryDeserializeClient(payload, out ClientMessage message, out _) == false)
            {
                _responses.SendRejected(connectionId, 0, RejectionReason.InvalidMessage);
                return;
            }
            RouteMessage(connectionId, message, serverTime);
        }

        public void Tick(double serverTime)
        {
            if (_sessionRegistry.isFull && MatchSystem.TryAdvanceTimeout(_match, serverTime))
                BroadcastSnapshots(serverTime);
        }

        private void RouteMessage(TransportConnectionId connectionId, ClientMessage message, double serverTime)
        {
            switch (message.type)
            {
                case MessageType.ConnectRequest:
                    AcceptConnection(connectionId, message, serverTime);
                    break;
                
                case MessageType.ResumeRequest:
                    ResumeConnection(connectionId, message, serverTime);
                    break;
                
                case MessageType.FireRequest:
                    ProcessFire(connectionId, message, serverTime);
                    break;
                
                case MessageType.StateRequest:
                    SendState(connectionId, message, serverTime);
                    break;
                
                case MessageType.HeartbeatRequest:
                    SendHeartbeat(connectionId, serverTime);
                    break;
                
                default:
                    _responses.SendRejected(connectionId, message.commandId, RejectionReason.InvalidMessage);
                    break;
            }
        }

        private void AcceptConnection(
            TransportConnectionId connectionId,
            ClientMessage message,
            double serverTime)
        {
            if (_sessionRegistry.TryAccept(
                    message.clientId,
                    connectionId,
                    out PlayerSession session,
                    out bool wasAdded) == false)
            {
                _responses.SendRejected(connectionId, 0, RejectionReason.ServerFull);
                return;
            }

            if (_sessionRegistry.isFull == false)
                return;

            if (wasAdded)
            {
                MatchSystem.ResetTurnDeadline(_match, serverTime);
                SendSessionAcceptedToAll(serverTime);
            }
            else
            {
                SendSessionAccepted(session, serverTime);
            }
        }

        private void ResumeConnection(
            TransportConnectionId connectionId,
            ClientMessage message,
            double serverTime)
        {
            if (_sessionRegistry.TryResume(message.clientId, connectionId, out PlayerSession session) == false)
            {
                _responses.SendRejected(connectionId, 0, RejectionReason.UnknownSession);
                return;
            }

            if (_sessionRegistry.isFull)
                SendSessionAccepted(session, serverTime);
        }

        private void ProcessFire(TransportConnectionId connectionId, ClientMessage message, double serverTime)
        {
            if (TryGetCurrentSession(connectionId, message.clientId, out PlayerSession session) == false)
            {
                _responses.SendRejected(connectionId, message.commandId, RejectionReason.UnknownSession);
                return;
            }

            if (session.TryGetProcessedResponse(message.commandId, out byte[] responsePayload))
            {
                _responses.Send(connectionId, responsePayload);
                return;
            }

            bool wasAccepted = MatchSystem.TryFire(
                _match,
                session.player,
                message.turnId,
                message.cellIndex,
                serverTime,
                out FireDecision decision);
            SendFireResult(session, message, decision, wasAccepted, serverTime);

            if (wasAccepted)
                BroadcastSnapshots(serverTime, session.clientId);
        }

        private void SendFireResult(
            PlayerSession session,
            ClientMessage request,
            FireDecision decision,
            bool wasAccepted,
            double serverTime)
        {
            PlayerSnapshot snapshot = PlayerSnapshotFactory.Create(_match, session.player, request.commandId);
            ServerMessage response = ServerMessage.CreateFireResult(
                _responses.ReserveMessageId(),
                request.commandId,
                wasAccepted ? RequestStatus.Accepted : RequestStatus.Rejected,
                FireDecisionStatus2RejectReason(decision.status),
                NetworkShotMapper.Convert(decision.outcome.result),
                decision.outcome.shipLength,
                snapshot,
                serverTime);
            byte[] payload = _responses.Serialize(response);
            session.RecordProcessedCommand(request.commandId, payload);
            _responses.Send(session.connectionId, payload);
        }
        
        private RejectionReason FireDecisionStatus2RejectReason(FireDecisionStatus status)
        {
            return status switch
            {
                FireDecisionStatus.Accepted => RejectionReason.None,
                FireDecisionStatus.MatchFinished => RejectionReason.MatchFinished,
                FireDecisionStatus.StaleTurn => RejectionReason.StaleTurn,
                FireDecisionStatus.WrongTurn => RejectionReason.WrongTurn,
                FireDecisionStatus.TurnExpired => RejectionReason.TurnExpired,
                FireDecisionStatus.InvalidTarget => RejectionReason.InvalidTarget,
                _ => RejectionReason.InvalidMessage
            };
        }

        private void SendState(
            TransportConnectionId connectionId,
            ClientMessage message,
            double serverTime)
        {
            if (TryGetCurrentSession(connectionId, message.clientId, out PlayerSession session) == false)
            {
                _responses.SendRejected(connectionId, 0, RejectionReason.UnknownSession);
                return;
            }
            SendSnapshot(session, serverTime);
        }

        private void SendSessionAccepted(PlayerSession session, double serverTime)
        {
            PlayerSnapshot snapshot = CreateSnapshot(session);
            ServerMessage response = ServerMessage.CreateSessionAccepted(
                _responses.ReserveMessageId(),
                NetworkPlayerSlotMapper.Convert(session.player),
                _definition,
                snapshot,
                serverTime);
            _responses.Send(session.connectionId, response);
        }

        private void SendSnapshot(PlayerSession session, double serverTime)
        {
            ServerMessage response = ServerMessage.CreateStateSnapshot(
                _responses.ReserveMessageId(),
                CreateSnapshot(session),
                serverTime);
            _responses.Send(session.connectionId, response);
        }

        private void BroadcastSnapshots(double serverTime, Guid excludedClientId = default)
        {
            foreach (PlayerSession session in _sessionRegistry.all)
            {
                if (session.clientId != excludedClientId)
                    SendSnapshot(session, serverTime);
            }
        }

        private void SendSessionAcceptedToAll(double serverTime)
        {
            foreach (PlayerSession session in _sessionRegistry.all)
            {
                SendSessionAccepted(session, serverTime);
            }
        }

        private void SendHeartbeat(TransportConnectionId connectionId, double serverTime)
        {
            _responses.SendHeartbeat(connectionId, serverTime);
        }

        private bool TryGetCurrentSession(
            TransportConnectionId connectionId,
            Guid clientId,
            out PlayerSession session)
        {
            return _sessionRegistry.TryGetCurrent(clientId, connectionId, out session);
        }

        private PlayerSnapshot CreateSnapshot(PlayerSession session)
        {
            return PlayerSnapshotFactory.Create(_match, session.player, session.lastProcessedCommandId);
        }
    }
}
