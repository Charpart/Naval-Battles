using System;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public interface IProtocolSerializer
    {
        byte[] Serialize(ClientMessage message);
        byte[] Serialize(ServerMessage message);

        bool TryDeserializeClient(
            ReadOnlyMemory<byte> payload,
            out ClientMessage message,
            out ProtocolError error);

        bool TryDeserializeServer(
            ReadOnlyMemory<byte> payload,
            out ServerMessage message,
            out ProtocolError error);
    }
}
