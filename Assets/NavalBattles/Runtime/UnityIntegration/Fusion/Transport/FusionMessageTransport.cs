using System;
using System.Collections.Generic;
using System.Threading;
using Fusion;
using Fusion.Sockets;
using NavalBattles.Runtime.Transport;

namespace NavalBattles.Runtime.UnityIntegration.Fusion.Transport
{
    public sealed class FusionMessageTransport : IMessageTransport, INetworkRunnerCallbacks, IDisposable
    {
        private const ulong CLIENT_TO_SERVER_CHANNEL = 1;
        private const ulong SERVER_TO_CLIENT_CHANNEL = 2;

        private readonly NetworkRunner _runner;
        private readonly bool _isServer;
        private readonly CancellationTokenRegistration _lifetimeRegistration;
        private ulong _nextSequence = 1;
        private bool _isReceivingEnabled = true;
        private bool _isDisposed;

        public event Action<TransportConnectionId, ReadOnlyMemory<byte>> OnReceived;

        private FusionMessageTransport(
            NetworkRunner runner,
            bool isServer,
            CancellationToken cancellationToken)
        {
            _runner = runner ? runner : throw new ArgumentNullException(nameof(runner));
            _isServer = isServer;
            _runner.AddCallbacks(this);
            _lifetimeRegistration = cancellationToken.Register(DisableReceiving);
        }

        public static FusionMessageTransport CreateServer(
            NetworkRunner runner,
            CancellationToken cancellationToken)
        {
            return new FusionMessageTransport(runner, true, cancellationToken);
        }

        public static FusionMessageTransport CreateClient(
            NetworkRunner runner,
            CancellationToken cancellationToken)
        {
            return new FusionMessageTransport(runner, false, cancellationToken);
        }

        public void Send(TransportConnectionId target, ReadOnlyMemory<byte> payload)
        {
            if (_isDisposed || _runner.IsRunning == false)
                return;

            ReliableKey key = CreateReliableKey();
            if (_isServer)
            {
                PlayerRef player = PlayerRef.FromRaw(target.value);
                _runner.SendReliableDataToPlayer(player, key, payload.Span);
                return;
            }
            _runner.SendReliableDataToServer(key, payload.Span);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _isReceivingEnabled = false;
            _lifetimeRegistration.Dispose();

            if (_runner)
                _runner.RemoveCallbacks(this);

            OnReceived = null;
        }

        public void OnReliableDataReceived(
            NetworkRunner runner,
            PlayerRef player,
            ReliableKey key,
            ReadOnlySpan<byte> data)
        {
            if (_isReceivingEnabled == false || runner != _runner)
                return;

            TransportConnectionId source = _isServer
                ? new TransportConnectionId(player.RawEncoded)
                : new TransportConnectionId(0);
            byte[] payload = data.ToArray();
            OnReceived?.Invoke(source, payload);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (runner == _runner)
                _isReceivingEnabled = false;
        }

        private ReliableKey CreateReliableKey()
        {
            ulong channel = _isServer ? SERVER_TO_CLIENT_CHANNEL : CLIENT_TO_SERVER_CHANNEL;
            return ReliableKey.FromULongs(channel, _nextSequence++);
        }

        private void DisableReceiving()
        {
            _isReceivingEnabled = false;
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
