using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using NavalBattles.Runtime.UnityIntegration.Fusion.Transport;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Fusion
{
    public sealed class FusionPeer
    {
        private bool _isShutdown;

        public NetworkRunner runner { get; }
        public FusionMessageTransport transport { get; }

        public FusionPeer(NetworkRunner runner, FusionMessageTransport transport)
        {
            this.runner = runner;
            this.transport = transport;
        }

        public async UniTask ShutdownAsync(CancellationToken cancellationToken)
        {
            if (_isShutdown)
                return;

            _isShutdown = true;
            transport.Dispose();

            if (runner != null && runner.IsShutdown == false)
                await runner.Shutdown();

            if (runner != null)
                Object.Destroy(runner.gameObject);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
