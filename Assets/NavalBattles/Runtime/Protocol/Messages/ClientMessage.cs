using System;

namespace NavalBattles.Runtime.Protocol.Messages
{
    public readonly struct ClientMessage
    {
        public MessageType type { get; }
        public Guid clientId { get; }
        public ulong messageId { get; }
        public ulong commandId { get; }
        public ulong turnId { get; }
        public int cellIndex { get; }
        public ulong knownRevision { get; }

        private ClientMessage(
            MessageType type,
            Guid clientId,
            ulong messageId,
            ulong commandId,
            ulong turnId,
            int cellIndex,
            ulong knownRevision)
        {
            this.type = type;
            this.clientId = clientId;
            this.messageId = messageId;
            this.commandId = commandId;
            this.turnId = turnId;
            this.cellIndex = cellIndex;
            this.knownRevision = knownRevision;
        }

        public static ClientMessage CreateConnectRequest(Guid clientId, ulong messageId)
        {
            return new ClientMessage(MessageType.ConnectRequest, clientId, messageId, 0, 0, -1, 0);
        }

        public static ClientMessage CreateResumeRequest(Guid clientId, ulong messageId, ulong knownRevision)
        {
            return new ClientMessage(MessageType.ResumeRequest, clientId, messageId, 0, 0, -1, knownRevision);
        }

        public static ClientMessage CreateFireRequest(
            Guid clientId,
            ulong messageId,
            ulong commandId,
            ulong turnId,
            int cellIndex)
        {
            return new ClientMessage(
                MessageType.FireRequest,
                clientId,
                messageId,
                commandId,
                turnId,
                cellIndex,
                0);
        }

        public static ClientMessage CreateStateRequest(Guid clientId, ulong messageId, ulong knownRevision)
        {
            return new ClientMessage(MessageType.StateRequest, clientId, messageId, 0, 0, -1, knownRevision);
        }

        public static ClientMessage CreateHeartbeatRequest(Guid clientId, ulong messageId)
        {
            return new ClientMessage(MessageType.HeartbeatRequest, clientId, messageId, 0, 0, -1, 0);
        }

        public static ClientMessage CreateDecoded(
            MessageType type,
            Guid clientId,
            ulong messageId,
            ulong commandId,
            ulong turnId,
            int cellIndex,
            ulong knownRevision)
        {
            return new ClientMessage(type, clientId, messageId, commandId, turnId, cellIndex, knownRevision);
        }
    }
}
