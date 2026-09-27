using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.Client.Commands
{
    public sealed class PendingFireCommand
    {
        private ulong _commandId;
        private int _cellIndex = -1;

        public byte[] payload { get; private set; }

        public bool exists => payload != null;

        public RequestStatus lastStatus { get; private set; }

        public RejectionReason lastRejectionReason { get; private set; }

        public void Begin(ulong commandId, int cellIndex, byte[] serializedPayload)
        {
            _commandId = commandId;
            _cellIndex = cellIndex;
            payload = serializedPayload;
            lastStatus = RequestStatus.None;
            lastRejectionReason = RejectionReason.None;
        }

        public bool TryComplete(
            ulong commandId,
            RequestStatus status,
            RejectionReason rejectionReason)
        {
            if (exists == false || commandId != _commandId)
                return false;

            lastStatus = status;
            lastRejectionReason = rejectionReason;
            ClearPendingData();

            return true;
        }

        public bool TryReconcile(PlayerSnapshot snapshot)
        {
            if (exists == false || snapshot == null || snapshot.lastProcessedCommandId < _commandId)
                return false;

            bool wasAccepted = _cellIndex >= 0
                && snapshot.opponentShots[_cellIndex] != NetworkShotState.None;
            lastStatus = wasAccepted ? RequestStatus.Accepted : RequestStatus.Rejected;
            lastRejectionReason = RejectionReason.None;
            ClearPendingData();

            return true;
        }

        private void ClearPendingData()
        {
            _commandId = 0;
            _cellIndex = -1;
            payload = null;
        }
    }
}
