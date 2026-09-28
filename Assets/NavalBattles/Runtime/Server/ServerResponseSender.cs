using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.Server
{
    internal sealed class ServerResponseSender
    {
        private readonly IMessageTransport _transport;
        private readonly IProtocolSerializer _serializer;
        private ulong _nextMessageId = 1;

        public ServerResponseSender(IMessageTransport transport, IProtocolSerializer serializer)
        {
            _transport = transport;
            _serializer = serializer;
        }

        public ulong ReserveMessageId()
        {
            return _nextMessageId++;
        }

        public byte[] Serialize(ServerMessage message)
        {
            return _serializer.Serialize(message);
        }

        public void Send(TransportConnectionId connectionId, ServerMessage message)
        {
            _transport.Send(connectionId, Serialize(message));
        }

        public void Send(TransportConnectionId connectionId, byte[] payload)
        {
            _transport.Send(connectionId, payload);
        }

        public void SendHeartbeat(
            TransportConnectionId connectionId,
            ulong requestMessageId,
            double serverTime)
        {
            ServerMessage response = ServerMessage.CreateHeartbeatResponse(
                requestMessageId,
                serverTime);
            Send(connectionId, response);
        }
        
        public void SendRejected(
            TransportConnectionId connectionId,
            ulong commandId,
            RejectionReason reason)
        {
            ServerMessage response = ServerMessage.CreateRequestRejected(
                ReserveMessageId(),
                commandId,
                reason);
            Send(connectionId, response);
        }
    }
}
