using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.Transport.Simulation;
using NavalBattles.Runtime.UnityIntegration.Fusion;
using NavalBattles.Runtime.UnityIntegration.Configuration;
using NavalBattles.Runtime.Transport.Diagnostics;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public sealed class ClientRuntime
    {
        private static readonly TransportConnectionId ServerConnection = new TransportConnectionId(0);
        private readonly FusionPeer _peer;
        private readonly NetworkSimulationTransport _simulationTransport;

        public GameClient client { get; }

        public event Action<TransportLogEntry> messageLogged
        {
            add => _simulationTransport.OnMessageLogged += value;
            remove => _simulationTransport.OnMessageLogged -= value;
        }

        public ClientRuntime(
            FusionPeer peer,
            IClientIdentityStore identityStore,
            ClientNetworkSettings networkSettings,
            int randomSeed)
        {
            _peer = peer ?? throw new ArgumentNullException(nameof(peer));
            networkSettings = networkSettings
                ?? throw new ArgumentNullException(nameof(networkSettings));
            _simulationTransport = new NetworkSimulationTransport(
                peer.transport,
                networkSettings.simulationSettings,
                randomSeed);
            client = new GameClient(
                _simulationTransport,
                identityStore,
                ServerConnection,
                new BinaryProtocolSerializer(),
                networkSettings.recoverySettings);
            _simulationTransport.OnReceived += OnReceived;
        }

        public void Connect()
        {
            client.Connect();
        }

        public void Tick(double clientTime)
        {
            _simulationTransport.Tick(clientTime);
            client.Tick(clientTime);
        }

        public void SetConnected(bool isConnected)
        {
            _simulationTransport.SetConnected(isConnected);
        }

        public void SetNetworkSimulationSettings(NetworkSimulationSettings settings)
        {
            _simulationTransport.SetSettings(settings);
        }

        public UniTask ShutdownAsync(CancellationToken cancellationToken)
        {
            _simulationTransport.OnReceived -= OnReceived;
            _simulationTransport.Dispose();

            return _peer.ShutdownAsync(cancellationToken);
        }

        private void OnReceived(TransportConnectionId source, ReadOnlyMemory<byte> payload)
        {
            client.Receive(payload);
        }
    }
}
