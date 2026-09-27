namespace NavalBattles.Runtime.Server.Commands
{
    public readonly struct ProcessedCommand
    {
        public ulong commandId { get; }
        public byte[] responsePayload { get; }

        public ProcessedCommand(ulong commandId, byte[] responsePayload)
        {
            this.commandId = commandId;
            this.responsePayload = responsePayload;
        }
    }
}
