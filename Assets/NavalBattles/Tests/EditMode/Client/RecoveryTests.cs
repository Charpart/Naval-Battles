using System;
using NavalBattles.Runtime.Client;
using NavalBattles.Runtime.Client.Identity;
using NavalBattles.Runtime.Client.Connection;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Protocol.Serialization;
using NavalBattles.Runtime.Server;
using NavalBattles.Runtime.Transport;
using NavalBattles.Tests.EditMode.Fakes;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Client
{
    [TestFixture]
    public sealed class RecoveryTests
    {
        private static readonly TransportConnectionId FirstConnection = new TransportConnectionId(10);
        private static readonly TransportConnectionId SecondConnection = new TransportConnectionId(20);
        private static readonly TransportConnectionId ServerConnection = new TransportConnectionId(0);
        private static readonly ClientRecoverySettings RecoverySettings =
            new ClientRecoverySettings(2.0, 5.0, 1.0);

        private IProtocolSerializer _serializer;
        private FakeMessageTransport _serverTransport;
        private GameServer _server;
        private FakeMessageTransport _firstTransport;
        private FakeMessageTransport _secondTransport;
        private GameClient _firstClient;
        private GameClient _secondClient;

        [SetUp]
        public void SetUp()
        {
            _serializer = new BinaryProtocolSerializer();
            _serverTransport = new FakeMessageTransport();
            _server = CreateServer();
            _firstTransport = new FakeMessageTransport();
            _secondTransport = new FakeMessageTransport();
            _firstClient = CreateClient(_firstTransport);
            _secondClient = CreateClient(_secondTransport);
            ConnectClients();
        }

        [Test]
        public void Retry_WhenFireResultIsLost_CompletesCommandWithoutSecondStateChange()
        {
            // Arrange
            GameClient activeClient = GetActiveClient();
            FakeMessageTransport activeTransport = GetTransport(activeClient);
            TransportConnectionId activeConnection = GetConnection(activeClient);
            ulong initialRevision = _server.stateRevision;
            Assert.That(activeClient.TryFire(0), Is.True);
            RouteToServer(activeTransport, activeConnection, 100.0);
            Assert.That(_server.stateRevision, Is.EqualTo(initialRevision + 1));
            _serverTransport.Clear();

            // Act
            activeClient.Tick(101.0);
            RouteToServer(activeTransport, activeConnection, 101.0);
            DeliverServerMessages();

            // Assert
            Assert.That(_server.stateRevision, Is.EqualTo(initialRevision + 1));
            Assert.That(activeClient.hasPendingCommand, Is.False);
            Assert.That(activeClient.snapshot.revision, Is.EqualTo(_server.stateRevision));
        }

        private GameServer CreateServer()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            bool wereRulesCreated = GameRules.TryCreate(
                6,
                6,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);
            Assert.That(wereRulesCreated, Is.True, error.ToString());

            return new GameServer(rules, 42, 100.0, _serverTransport, _serializer);
        }

        private GameClient CreateClient(FakeMessageTransport transport)
        {
            var client = new GameClient(
                transport,
                new ClientIdentityStore(Guid.NewGuid()),
                ServerConnection,
                _serializer,
                RecoverySettings);
            client.Tick(100.0);

            return client;
        }

        private void ConnectClients()
        {
            _firstClient.Connect();
            RouteToServer(_firstTransport, FirstConnection, 100.0);
            _secondClient.Connect();
            RouteToServer(_secondTransport, SecondConnection, 100.0);
            DeliverServerMessages();
            _firstTransport.Clear();
            _secondTransport.Clear();
        }

        private void RouteToServer(
            FakeMessageTransport transport,
            TransportConnectionId connection,
            double serverTime)
        {
            foreach (SentMessage message in transport.sentMessages)
            {
                _server.Receive(connection, message.payload, serverTime);
            }

            transport.Clear();
        }

        private void DeliverServerMessages()
        {
            foreach (SentMessage message in _serverTransport.sentMessages)
            {
                if (message.target == FirstConnection)
                {
                    _firstClient.Receive(message.payload);
                }
                else if (message.target == SecondConnection)
                {
                    _secondClient.Receive(message.payload);
                }
            }

            _serverTransport.Clear();
        }

        private GameClient GetActiveClient()
        {
            return _firstClient.snapshot.activePlayer == _firstClient.player
                ? _firstClient
                : _secondClient;
        }

        private FakeMessageTransport GetTransport(GameClient client)
        {
            return ReferenceEquals(client, _firstClient) ? _firstTransport : _secondTransport;
        }

        private TransportConnectionId GetConnection(GameClient client)
        {
            return ReferenceEquals(client, _firstClient) ? FirstConnection : SecondConnection;
        }
    }
}
