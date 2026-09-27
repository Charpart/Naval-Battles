using System;

namespace NavalBattles.Runtime.Client.Connection
{
    public sealed class ClientRecoveryTimer
    {
        private readonly ClientRecoverySettings _settings;
        private double _currentTime;
        private double _lastServerMessageTime;
        private double _nextHeartbeatTime;
        private double _nextRetryTime;

        public ClientRecoveryTimer(ClientRecoverySettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Advance(double clientTime)
        {
            _currentTime = clientTime;
        }

        public void RecordServerActivity()
        {
            _lastServerMessageTime = _currentTime;
            _nextHeartbeatTime = _currentTime + _settings.heartbeatIntervalSeconds;
        }

        public void RecordHeartbeatSent()
        {
            _nextHeartbeatTime = _currentTime + _settings.heartbeatIntervalSeconds;
        }

        public void RecordCommandSent()
        {
            _nextRetryTime = _currentTime + _settings.commandRetryIntervalSeconds;
        }

        public bool IsTimedOut(ClientConnectionState connectionState)
        {
            return connectionState != ClientConnectionState.Disconnected
                && _currentTime - _lastServerMessageTime >= _settings.heartbeatTimeoutSeconds;
        }

        public bool IsHeartbeatDue(ClientConnectionState connectionState)
        {
            return connectionState != ClientConnectionState.Disconnected
                && _currentTime >= _nextHeartbeatTime;
        }

        public bool IsRetryDue(ClientConnectionState connectionState, bool hasPendingCommand)
        {
            return connectionState == ClientConnectionState.Connected
                && hasPendingCommand
                && _currentTime >= _nextRetryTime;
        }

        public bool IsSessionRetryDue(ClientConnectionState connectionState, bool hasPendingSession)
        {
            return hasPendingSession && connectionState is ClientConnectionState.Connecting
                    or ClientConnectionState.Reconnecting && _currentTime >= _nextRetryTime;
        }
    }
}
