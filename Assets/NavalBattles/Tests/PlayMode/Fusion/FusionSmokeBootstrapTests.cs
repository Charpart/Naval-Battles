using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NavalBattles.Runtime.Unity.Fusion;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace NavalBattles.Tests.PlayMode.Fusion
{
    [TestFixture]
    public sealed class FusionSmokeBootstrapTests
    {
        private const int NETWORK_SCENE_BUILD_INDEX = 0;

        private FusionSmokeBootstrap _bootstrap;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrap != null)
            {
                yield return _bootstrap.ShutdownAsync(CancellationToken.None).ToCoroutine();
            }
        }

        [UnityTest]
        [Timeout(90000)]
        public IEnumerator StartAsync_WhenPhotonIsConfigured_StartsServerAndTwoClients()
        {
            // Arrange
            string sessionName = $"naval-smoke-{System.Guid.NewGuid():N}";
            _bootstrap = new FusionSmokeBootstrap(sessionName, NETWORK_SCENE_BUILD_INDEX);

            // Act
            yield return _bootstrap.StartAsync(CancellationToken.None).ToCoroutine();

            // Assert
            Assert.That(_bootstrap.isServerRunning, Is.True);
            Assert.That(_bootstrap.connectedClientCount, Is.EqualTo(2));
        }
    }
}
