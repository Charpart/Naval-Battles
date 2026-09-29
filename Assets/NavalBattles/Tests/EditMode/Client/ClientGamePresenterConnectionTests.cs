using System;
using System.Linq;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Protocol.Snapshots;
using NavalBattles.Runtime.Transport;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using NavalBattles.Runtime.UnityIntegration.Presentation.Presenters;
using NavalBattles.Runtime.UnityIntegration.Presentation.Views;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Client
{
    [TestFixture]
    public sealed class ClientGamePresenterConnectionTests
    {
        private static readonly TransportConnectionId ServerConnection = new TransportConnectionId(0);
        private static readonly ClientRecoverySettings RecoverySettings =
            new ClientRecoverySettings(2.0, 5.0, 1.0);

        private IProtocolSerializer _serializer;
        private GameClient _client;
        private FakeGameView _view;
        private ClientGamePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _serializer = new BinaryProtocolSerializer();
            _client = new GameClient(
                new FakeMessageTransport(),
                new ClientIdentityStore(Guid.NewGuid()),
                ServerConnection,
                _serializer,
                RecoverySettings);
            _client.Tick(100.0);
            _client.Connect();
            _client.Receive(_serializer.Serialize(CreateSessionAccepted()));
            _view = new FakeGameView();
            _presenter = new ClientGamePresenter(_client, _view);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        [Test]
        public void Refresh_WhenHeartbeatTimesOut_ShowsLocalPlayerAsDisconnected()
        {
            // Act
            _client.Tick(105.0);
            _presenter.Refresh(105.0);

            // Assert
            PlayerStatusModel localPlayer = GetLocalPlayer();
            Assert.That(_client.connectionState, Is.EqualTo(ClientConnectionState.Disconnected));
            Assert.That(localPlayer.status, Is.EqualTo(PlayerGameStatus.Disconnected));
        }

        [Test]
        public void Refresh_WhenDisconnectedClientConnects_ShowsLocalPlayerAsReconnecting()
        {
            // Arrange
            _client.Tick(105.0);

            // Act
            _client.Connect();
            _presenter.Refresh(105.0);

            // Assert
            PlayerStatusModel localPlayer = GetLocalPlayer();
            Assert.That(_client.connectionState, Is.EqualTo(ClientConnectionState.Reconnecting));
            Assert.That(localPlayer.status, Is.EqualTo(PlayerGameStatus.Reconnecting));
        }

        private PlayerStatusModel GetLocalPlayer()
        {
            return _view.model.players.Single(player => player.isLocal);
        }

        private static ServerMessage CreateSessionAccepted()
        {
            var definition = new GameDefinition(2, 2, new[] { 1 }, 15.0);
            var snapshot = new PlayerSnapshot(
                1,
                1,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.First,
                NetworkPlayerSlot.None,
                115.0,
                0,
                new sbyte[] { 0, -1, -1, -1 },
                new NetworkShotState[4],
                new NetworkShotState[4],
                new[] { 0 },
                new[] { false });

            return ServerMessage.CreateSessionAccepted(
                1,
                NetworkPlayerSlot.First,
                definition,
                snapshot,
                100.0);
        }

        private sealed class FakeGameView : IGameView
        {
            public event Action<int> targetCellSelected
            {
                add { }
                remove { }
            }

            public GamePresentationModel model { get; private set; }

            public void RenderWaiting(string firstPlayerLabel, string secondPlayerLabel)
            {
                model = null;
            }

            public void Render(GamePresentationModel renderedModel)
            {
                model = renderedModel;
            }
        }
    }
}
