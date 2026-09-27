using System;

namespace NavalBattles.Runtime.Client.Identity
{
    public sealed class ClientIdentityStore : IClientIdentityStore
    {
        private ulong _nextCommandId = 1;

        public Guid clientId { get; }

        public ClientIdentityStore(Guid clientId)
        {
            if (clientId == Guid.Empty)
            {
                throw new ArgumentException("Client identifier must not be empty.", nameof(clientId));
            }

            this.clientId = clientId;
        }

        public ulong ReserveCommandId()
        {
            return _nextCommandId++;
        }
    }
}
