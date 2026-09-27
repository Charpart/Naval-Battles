using System;

namespace NavalBattles.Runtime.Client.Identity
{
    public interface IClientIdentityStore
    {
        public Guid clientId { get; }

        public ulong ReserveCommandId();
    }
}
