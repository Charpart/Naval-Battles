using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Server;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.UnityIntegration.Fusion;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public sealed class ServerRuntime
    {
        private readonly FusionPeer _peer;
        private readonly IElapsedTimeProvider _timeProvider;

        public GameServer server { get; }

        public ServerRuntime(
            FusionPeer peer,
            GameRules rules,
            int seed,
            IElapsedTimeProvider timeProvider)
        {
            _peer = peer ?? throw new ArgumentNullException(nameof(peer));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            server = new GameServer(
                rules,
                seed,
                timeProvider.elapsedSeconds,
                peer.transport,
                new BinaryProtocolSerializer());
            _peer.transport.OnReceived += OnReceived;
        }

        public void Tick(double serverTime)
        {
            server.Tick(serverTime);
        }

        public UniTask ShutdownAsync(CancellationToken cancellationToken)
        {
            _peer.transport.OnReceived -= OnReceived;
            return _peer.ShutdownAsync(cancellationToken);
        }

        private void OnReceived(TransportConnectionId source, ReadOnlyMemory<byte> payload)
        {
            server.Receive(source, payload, _timeProvider.elapsedSeconds);
        }
    }
}
