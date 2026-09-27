using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Server.Commands;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.Server.Connections
{
    public sealed class PlayerSession
    {
        private readonly Dictionary<ulong, ProcessedCommand> _processedCommands = new();

        public Guid clientId { get; }
        public PlayerSlot player { get; }
        public TransportConnectionId connectionId { get; private set; }
        public ulong lastProcessedCommandId { get; private set; }

        public PlayerSession(Guid clientId, PlayerSlot player, TransportConnectionId connectionId)
        {
            this.clientId = clientId;
            this.player = player;
            this.connectionId = connectionId;
        }

        public void Rebind(TransportConnectionId connectionId)
        {
            this.connectionId = connectionId;
        }

        public bool TryGetProcessedResponse(ulong commandId, out byte[] responsePayload)
        {
            if (_processedCommands.TryGetValue(commandId, out ProcessedCommand command))
            {
                responsePayload = command.responsePayload;
                return true;
            }

            responsePayload = null;
            return false;
        }

        public void RecordProcessedCommand(ulong commandId, byte[] responsePayload)
        {
            _processedCommands[commandId] = new ProcessedCommand(commandId, responsePayload);
            lastProcessedCommandId = commandId;
        }
    }
}
