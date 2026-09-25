using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NavalBattles.Runtime.Protocol.Snapshots
{
    public sealed class GameDefinition
    {
        private readonly ReadOnlyCollection<int> _shipLengths;

        public int width { get; }
        public int height { get; }
        public double turnDurationSeconds { get; }
        
        public IReadOnlyList<int> shipLengths => _shipLengths;

        public GameDefinition(int width, int height, int[] shipLengths, double turnDurationSeconds)
        {
            this.width = width;
            this.height = height;
            this.turnDurationSeconds = turnDurationSeconds;
            _shipLengths = Array.AsReadOnly((int[])shipLengths.Clone());
        }
    }
}
