using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NavalBattles.Runtime.Domain.Configuration
{
    public sealed class GameRules
    {
        private readonly ReadOnlyCollection<int> _shipLengths;

        public int width { get; }

        public int height { get; }

        public int cellCount { get; }

        public double turnDurationSeconds { get; }

        public IReadOnlyList<int> shipLengths => _shipLengths;

        private GameRules(int width, int height, int[] shipLengths, double turnDurationSeconds)
        {
            this.width = width;
            this.height = height;
            cellCount = width * height;
            this.turnDurationSeconds = turnDurationSeconds;
            _shipLengths = Array.AsReadOnly(shipLengths);
        }

        public static bool TryCreate(
            int width,
            int height,
            IReadOnlyList<int> shipLengths,
            double turnDurationSeconds,
            out GameRules rules,
            out GameRulesValidationError validationError)
        {
            rules = null;

            if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
            {
                validationError = GameRulesValidationError.InvalidBoardSize;

                return false;
            }

            if (shipLengths == null || shipLengths.Count == 0)
            {
                validationError = GameRulesValidationError.EmptyFleet;

                return false;
            }

            if (turnDurationSeconds <= 0.0 || double.IsNaN(turnDurationSeconds) || double.IsInfinity(turnDurationSeconds))
            {
                validationError = GameRulesValidationError.InvalidTurnDuration;

                return false;
            }

            int maximumShipLength = Math.Max(width, height);
            int occupiedCellCount = 0;
            int[] copiedShipLengths = new int[shipLengths.Count];

            for (int shipIndex = 0; shipIndex < shipLengths.Count; shipIndex++)
            {
                int shipLength = shipLengths[shipIndex];

                if (shipLength <= 0 || shipLength > maximumShipLength)
                {
                    validationError = GameRulesValidationError.InvalidShipLength;

                    return false;
                }

                copiedShipLengths[shipIndex] = shipLength;
                occupiedCellCount += shipLength;
            }

            if (occupiedCellCount > width * height)
            {
                validationError = GameRulesValidationError.FleetDoesNotFit;

                return false;
            }

            rules = new GameRules(width, height, copiedShipLengths, turnDurationSeconds);
            validationError = GameRulesValidationError.None;

            return true;
        }
    }
}
