namespace NavalBattles.Runtime.Protocol.Messages
{
    public enum ProtocolError : byte
    {
        None = 0,
        UnsupportedVersion = 1,
        UnknownMessageType = 2,
        UnexpectedEnd = 3,
        InvalidCollectionLength = 4,
        MalformedPayload = 5,
        InvalidPayloadLength = 6
    }
}
