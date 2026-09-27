using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Transport.Diagnostics;

namespace NavalBattles.Runtime.Transport.Simulation
{
    public sealed class NetworkSimulationTransport : IMessageTransport, IDisposable
    {
        private readonly IMessageTransport _inner;
        private readonly Random _random;
        private readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>();
        private NetworkSimulationSettings _settings;
        private double _currentTime;
        private bool _isConnected = true;
        private bool _isDisposed;

        public event Action<TransportConnectionId, ReadOnlyMemory<byte>> OnReceived;
        public event Action<TransportLogEntry> OnMessageLogged;

        public NetworkSimulationTransport(IMessageTransport inner, NetworkSimulationSettings settings, int seed)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(seed);
            _inner.OnReceived += OnInnerReceived;
        }

        public void SetSettings(NetworkSimulationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void SetConnected(bool isConnected)
        {
            if (_isConnected == isConnected)
                return;

            _isConnected = isConnected;
            if (isConnected == false)
                DropScheduledMessages();
        }

        public void Send(TransportConnectionId target, ReadOnlyMemory<byte> payload)
        {
            Schedule(TransportMessageDirection.Outgoing, target, payload);
        }

        public void Tick(double time)
        {
            if (time < _currentTime)
                throw new ArgumentOutOfRangeException(nameof(time));

            _currentTime = time;
            while (_scheduledMessages.Count > 0 && _scheduledMessages[0].deliveryTime <= time)
            {
                ScheduledMessage message = _scheduledMessages[0];
                _scheduledMessages.RemoveAt(0);
                Deliver(message);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _inner.OnReceived -= OnInnerReceived;
            _scheduledMessages.Clear();
            
            OnReceived = null;
            OnMessageLogged = null;
        }

        private void OnInnerReceived(TransportConnectionId source, ReadOnlyMemory<byte> payload)
        {
            Schedule(TransportMessageDirection.Incoming, source, payload);
        }

        private void Schedule(
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            ReadOnlyMemory<byte> payload)
        {
            if (_isDisposed || _isConnected == false || IsShouldTrigger(_settings.lossProbability))
            {
                Log(TransportMessageStatus.Dropped, direction, connectionId, payload);
                return;
            }

            Enqueue(direction, connectionId, payload);
            LogScheduled(direction, connectionId, payload);

            if (IsShouldTrigger(_settings.duplicationProbability))
            {
                Enqueue(direction, connectionId, payload);
                Log(TransportMessageStatus.Duplicated, direction, connectionId, payload);
            }
        }

        private void Enqueue(
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            ReadOnlyMemory<byte> payload)
        {
            double jitter = (_random.NextDouble() * 2.0 - 1.0) * _settings.jitterSeconds;
            double deliveryTime = _currentTime + Math.Max(0.0, _settings.delaySeconds + jitter);
            var message = new ScheduledMessage(direction, connectionId, payload.ToArray(), deliveryTime);
            int insertionIndex = InsertByDeliveryTime(deliveryTime);
            _scheduledMessages.Insert(insertionIndex, message);
        }

        private int InsertByDeliveryTime(double deliveryTime)
        {
            int index = _scheduledMessages.Count;
            while (index > 0 && _scheduledMessages[index - 1].deliveryTime > deliveryTime)
            {
                index--;
            }
            return index;
        }

        private void Deliver(ScheduledMessage message)
        {
            if (message.direction == TransportMessageDirection.Outgoing)
            {
                _inner.Send(message.connectionId, message.payload);
                return;
            }

            OnReceived?.Invoke(message.connectionId, message.payload);
            Log(TransportMessageStatus.Received, message.direction, message.connectionId, message.payload);
        }

        private void DropScheduledMessages()
        {
            for (int index = 0; index < _scheduledMessages.Count; index++)
            {
                ScheduledMessage message = _scheduledMessages[index];
                Log(TransportMessageStatus.Dropped, message.direction, message.connectionId, message.payload);
            }
            _scheduledMessages.Clear();
        }

        private void LogScheduled(
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            ReadOnlyMemory<byte> payload)
        {
            if (direction == TransportMessageDirection.Outgoing)
            {
                Log(TransportMessageStatus.Sent, direction, connectionId, payload);
            }
        }

        private void Log(
            TransportMessageStatus status,
            TransportMessageDirection direction,
            TransportConnectionId connectionId,
            ReadOnlyMemory<byte> payload)
        {
            if (OnMessageLogged == null)
                return;

            OnMessageLogged.Invoke(new TransportLogEntry(
                status,
                direction,
                connectionId,
                payload,
                _currentTime));
        }

        private bool IsShouldTrigger(double probability)
        {
            return probability >= 1.0 || probability > 0.0 && _random.NextDouble() < probability;
        }
    }
}
