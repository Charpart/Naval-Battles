using System;

namespace NavalBattles.Runtime.Client.Connection
{
    public sealed class ClientRecoverySettings
    {
        public double heartbeatIntervalSeconds { get; }
        public double heartbeatTimeoutSeconds { get; }
        public double commandRetryIntervalSeconds { get; }

        public ClientRecoverySettings(
            double heartbeatIntervalSeconds,
            double heartbeatTimeoutSeconds,
            double commandRetryIntervalSeconds)
        {
            EnsurePositive(heartbeatIntervalSeconds, nameof(heartbeatIntervalSeconds));
            EnsurePositive(heartbeatTimeoutSeconds, nameof(heartbeatTimeoutSeconds));
            EnsurePositive(commandRetryIntervalSeconds, nameof(commandRetryIntervalSeconds));

            if (heartbeatTimeoutSeconds <= heartbeatIntervalSeconds)
            {
                throw new ArgumentException(
                    "Heartbeat timeout must be greater than its interval.",
                    nameof(heartbeatTimeoutSeconds));
            }

            this.heartbeatIntervalSeconds = heartbeatIntervalSeconds;
            this.heartbeatTimeoutSeconds = heartbeatTimeoutSeconds;
            this.commandRetryIntervalSeconds = commandRetryIntervalSeconds;
        }

        private static void EnsurePositive(double value, string parameterName)
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be positive.");
            }
        }
    }
}
