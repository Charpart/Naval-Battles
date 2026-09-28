using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Domain.Matches;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.Server.Connections
{
    public sealed class PlayerSessionRegistry
    {
        private const int MAXIMUM_PLAYER_COUNT = 2;

        private readonly Dictionary<Guid, PlayerSession> _sessionsByClient =
            new Dictionary<Guid, PlayerSession>();

        public int count => _sessionsByClient.Count;
        public bool isFull => count == MAXIMUM_PLAYER_COUNT;

        public IEnumerable<PlayerSession> all => _sessionsByClient.Values;

        public bool HasPlayer(PlayerSlot player)
        {
            foreach (PlayerSession session in _sessionsByClient.Values)
            {
                if (session.player == player)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAccept(
            Guid clientId,
            TransportConnectionId connectionId,
            out PlayerSession session,
            out bool wasAdded)
        {
            wasAdded = false;

            if (_sessionsByClient.TryGetValue(clientId, out session))
            {
                session.Rebind(connectionId);
                return true;
            }

            if (isFull)
                return false;

            PlayerSlot player = count == 0 ? PlayerSlot.First : PlayerSlot.Second;
            session = new PlayerSession(clientId, player, connectionId);
            _sessionsByClient.Add(clientId, session);
            wasAdded = true;

            return true;
        }

        public bool TryResume(
            Guid clientId,
            TransportConnectionId connectionId,
            out PlayerSession session)
        {
            if (_sessionsByClient.TryGetValue(clientId, out session) == false)
                return false;

            session.Rebind(connectionId);
            return true;
        }

        public bool TryGetCurrent(Guid clientId,
            TransportConnectionId connectionId,
            out PlayerSession session)
        {
            return _sessionsByClient.TryGetValue(clientId, out session)
                && session.connectionId == connectionId;
        }
    }
}
