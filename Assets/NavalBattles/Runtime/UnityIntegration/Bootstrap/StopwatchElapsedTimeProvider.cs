using System.Diagnostics;

namespace NavalBattles.Runtime.UnityIntegration.Bootstrap
{
    public sealed class StopwatchElapsedTimeProvider : IElapsedTimeProvider
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public double elapsedSeconds => _stopwatch.Elapsed.TotalSeconds;
    }
}
