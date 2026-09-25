using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Protocol.Messages
{
    public readonly struct ServerMessage
    {
        public MessageType type { get; }

        public ulong messageId { get; }
        public ulong commandId { get; }

        public NetworkPlayerSlot player { get; }
        public RequestStatus requestStatus { get; }
        public RejectionReason rejectionReason { get; }
        public NetworkShotResult shotResult { get; }

        public int sunkShipLength { get; }
        public double serverTime { get; }
        
        public GameDefinition definition { get; }
        public PlayerSnapshot snapshot { get; }

        private ServerMessage(
            MessageType type,
            ulong messageId,
            ulong commandId,
            NetworkPlayerSlot player,
            RequestStatus requestStatus,
            RejectionReason rejectionReason,
            NetworkShotResult shotResult,
            int sunkShipLength,
            double serverTime,
            GameDefinition definition,
            PlayerSnapshot snapshot)
        {
            this.type = type;
            this.messageId = messageId;
            this.commandId = commandId;
            this.player = player;
            this.requestStatus = requestStatus;
            this.rejectionReason = rejectionReason;
            this.shotResult = shotResult;
            this.sunkShipLength = sunkShipLength;
            this.serverTime = serverTime;
            this.definition = definition;
            this.snapshot = snapshot;
        }

        public static ServerMessage CreateSessionAccepted(
            ulong messageId,
            NetworkPlayerSlot player,
            GameDefinition definition,
            PlayerSnapshot snapshot)
        {
            return new ServerMessage(
                MessageType.SessionAccepted,
                messageId,
                0,
                player,
                RequestStatus.Accepted,
                RejectionReason.None,
                NetworkShotResult.Invalid,
                0,
                0.0,
                definition,
                snapshot);
        }

        public static ServerMessage CreateFireResult(
            ulong messageId,
            ulong commandId,
            RequestStatus requestStatus,
            RejectionReason rejectionReason,
            NetworkShotResult shotResult,
            int sunkShipLength,
            PlayerSnapshot snapshot)
        {
            return new ServerMessage(
                MessageType.FireResult,
                messageId,
                commandId,
                NetworkPlayerSlot.None,
                requestStatus,
                rejectionReason,
                shotResult,
                sunkShipLength,
                0.0,
                null,
                snapshot);
        }

        public static ServerMessage CreateStateSnapshot(ulong messageId, PlayerSnapshot snapshot)
        {
            return new ServerMessage(
                MessageType.StateSnapshot,
                messageId,
                0,
                NetworkPlayerSlot.None,
                RequestStatus.None,
                RejectionReason.None,
                NetworkShotResult.Invalid,
                0,
                0.0,
                null,
                snapshot);
        }

        public static ServerMessage CreateHeartbeatResponse(ulong messageId, double serverTime)
        {
            return new ServerMessage(
                MessageType.HeartbeatResponse,
                messageId,
                0,
                NetworkPlayerSlot.None,
                RequestStatus.None,
                RejectionReason.None,
                NetworkShotResult.Invalid,
                0,
                serverTime,
                null,
                null);
        }

        public static ServerMessage CreateRequestRejected(
            ulong messageId,
            ulong commandId,
            RejectionReason rejectionReason)
        {
            return new ServerMessage(
                MessageType.RequestRejected,
                messageId,
                commandId,
                NetworkPlayerSlot.None,
                RequestStatus.Rejected,
                rejectionReason,
                NetworkShotResult.Invalid,
                0,
                0.0,
                null,
                null);
        }

        public static ServerMessage CreateDecoded(
            MessageType type,
            ulong messageId,
            ulong commandId,
            NetworkPlayerSlot player,
            RequestStatus requestStatus,
            RejectionReason rejectionReason,
            NetworkShotResult shotResult,
            int sunkShipLength,
            double serverTime,
            GameDefinition definition,
            PlayerSnapshot snapshot)
        {
            return new ServerMessage(
                type,
                messageId,
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
