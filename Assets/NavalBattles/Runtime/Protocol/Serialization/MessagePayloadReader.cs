using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public sealed class MessagePayloadReader
    {
        private readonly LittleEndianReader _reader;
        private readonly SnapshotPayloadReader _snapshotReader;

        public ProtocolError error => _reader.error;

        public bool hasRemainingBytes => _reader.hasRemainingBytes;

        public MessagePayloadReader(ReadOnlyMemory<byte> bytes)
        {
            _reader = new LittleEndianReader(bytes);
            _snapshotReader = new SnapshotPayloadReader(_reader);
        }

        public bool TryReadClient(ProtocolHeader header, out ClientMessage message)
        {
            message = default;

            switch (header.type)
            {
                case MessageType.ConnectRequest:
                case MessageType.HeartbeatRequest:
                    message = CreateClientMessage(header);
                    return true;

                case MessageType.ResumeRequest:
                case MessageType.StateRequest:
                    return TryReadRevisionRequest(header, out message);

                case MessageType.FireRequest:
                    return TryReadFireRequest(header, out message);

                default:
                    return false;
            }
        }

        private static ClientMessage CreateClientMessage(ProtocolHeader header)
        {
            return ClientMessage.CreateDecoded(
                header.type,
                header.clientId,
                header.messageId,
                0,
                0,
                -1,
                0);
        }

        public bool TryReadServer(ProtocolHeader header, out ServerMessage message)
        {
            switch (header.type)
            {
                case MessageType.SessionAccepted:
                    return TryReadSessionAccepted(header, out message);

                case MessageType.FireResult:
                    return TryReadFireResult(header, out message);

                case MessageType.StateSnapshot:
                    return TryReadStateSnapshot(header, out message);

                case MessageType.HeartbeatResponse:
                    return TryReadHeartbeatResponse(header, out message);

                case MessageType.RequestRejected:
                    return TryReadRequestRejected(header, out message);

                default:
                    message = default;
                    return false;
            }
        }

        private bool TryReadRevisionRequest(ProtocolHeader header, out ClientMessage message)
        {
            message = default;

            if (_reader.TryReadUInt64(out ulong knownRevision) == false)
            {
                return false;
            }

            message = ClientMessage.CreateDecoded(
                header.type,
                header.clientId,
                header.messageId,
                0,
                0,
                -1,
                knownRevision);

            return true;
        }

        private bool TryReadFireRequest(ProtocolHeader header, out ClientMessage message)
        {
            message = default;

            if (_reader.TryReadUInt64(out ulong commandId) == false ||
                _reader.TryReadUInt64(out ulong turnId) == false ||
                _reader.TryReadInt32(out int cellIndex) == false)
            {
                return false;
            }

            message = ClientMessage.CreateDecoded(
                header.type,
                header.clientId,
                header.messageId,
                commandId,
                turnId,
                cellIndex,
                0);

            return true;
        }

        private bool TryReadSessionAccepted(ProtocolHeader header, out ServerMessage message)
        {
            message = default;

            if (_reader.TryReadByte(out byte player) == false ||
                _snapshotReader.TryReadDefinition(out GameDefinition definition) == false ||
                _snapshotReader.TryReadSnapshot(out PlayerSnapshot snapshot) == false)
            {
                return false;
            }

            message = CreateServerMessage(
                header,
                player: (NetworkPlayerSlot)player,
                definition: definition,
                snapshot: snapshot);

            return true;
        }

        private bool TryReadFireResult(ProtocolHeader header, out ServerMessage message)
        {
            message = default;

            if (_reader.TryReadUInt64(out ulong commandId) == false ||
                _reader.TryReadByte(out byte status) == false ||
                _reader.TryReadByte(out byte rejection) == false ||
                _reader.TryReadByte(out byte shotResult) == false ||
                _reader.TryReadInt32(out int sunkShipLength) == false ||
                _snapshotReader.TryReadSnapshot(out PlayerSnapshot snapshot) == false)
            {
                return false;
            }

            message = CreateServerMessage(
                header,
                commandId,
                (RequestStatus)status,
                (RejectionReason)rejection,
                (NetworkShotResult)shotResult,
                sunkShipLength,
                snapshot: snapshot);

            return true;
        }

        private bool TryReadStateSnapshot(ProtocolHeader header, out ServerMessage message)
        {
            message = default;

            if (_snapshotReader.TryReadSnapshot(out PlayerSnapshot snapshot) == false)
            {
                return false;
            }

            message = CreateServerMessage(header, snapshot: snapshot);

            return true;
        }

        private bool TryReadHeartbeatResponse(ProtocolHeader header, out ServerMessage message)
        {
            message = default;

            if (_reader.TryReadDouble(out double serverTime) == false)
            {
                return false;
            }

            message = CreateServerMessage(header, serverTime: serverTime);

            return true;
        }

        private bool TryReadRequestRejected(ProtocolHeader header, out ServerMessage message)
        {
            message = default;

            if (_reader.TryReadUInt64(out ulong commandId) == false ||
                _reader.TryReadByte(out byte rejection) == false)
            {
                return false;
            }

            message = CreateServerMessage(
                header,
                commandId,
                RequestStatus.Rejected,
                (RejectionReason)rejection);

            return true;
        }

        private static ServerMessage CreateServerMessage(
            ProtocolHeader header,
            ulong commandId = 0,
            RequestStatus requestStatus = RequestStatus.None,
            RejectionReason rejectionReason = RejectionReason.None,
            NetworkShotResult shotResult = NetworkShotResult.Invalid,
            int sunkShipLength = 0,
            double serverTime = 0.0,
            NetworkPlayerSlot player = NetworkPlayerSlot.None,
            GameDefinition definition = null,
            PlayerSnapshot snapshot = null)
        {
            return ServerMessage.CreateDecoded(
                header.type,
                header.messageId,
                commandId,
                player,
                requestStatus,
                rejectionReason,
                shotResult,
                sunkShipLength,
                serverTime,
                definition,
                snapshot);
        }
    }
}
