using System;
using System.Collections.Generic;
using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;

namespace NavalBattles.Runtime.Domain.Fleets
{
    public static class FleetPlacementSystem
    {
        private const sbyte EMPTY_CELL = -1;

        public static bool TryCreateBoard(GameRules rules, int seed, out Board board)
        {
            board = null;

            if (rules == null || rules.shipLengths.Count > sbyte.MaxValue)
            {
                return false;
            }

            sbyte[] shipIndicesByCell = new sbyte[rules.cellCount];
            Array.Fill(shipIndicesByCell, EMPTY_CELL);
            ShipState[] ships = new ShipState[rules.shipLengths.Count];

            for (int shipIndex = 0; shipIndex < ships.Length; shipIndex++)
            {
                ships[shipIndex] = new ShipState(rules.shipLengths[shipIndex], 0);
            }

            DeterministicRandom random = new DeterministicRandom(seed);
            bool wasPlaced = TryPlaceShip(0, rules, shipIndicesByCell, ref random);

            if (wasPlaced == false)
            {
                return false;
            }

            board = new Board(rules.width, rules.height, shipIndicesByCell, ships);

            return true;
        }

        private static bool TryPlaceShip(
            int shipIndex,
            GameRules rules,
            sbyte[] shipIndicesByCell,
            ref DeterministicRandom random)
        {
            if (shipIndex >= rules.shipLengths.Count)
            {
                return true;
            }

            int shipLength = rules.shipLengths[shipIndex];
            List<Placement> placements = CreatePlacements(rules.width, rules.height, shipLength);
            Shuffle(placements, ref random);

            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                Placement placement = placements[placementIndex];

                if (CanPlace(placement, shipLength, shipIndicesByCell) == false)
                {
                    continue;
                }

                SetPlacement(placement, shipLength, (sbyte)shipIndex, shipIndicesByCell);

                if (TryPlaceShip(shipIndex + 1, rules, shipIndicesByCell, ref random))
                {
                    return true;
                }

                SetPlacement(placement, shipLength, EMPTY_CELL, shipIndicesByCell);
            }

            return false;
        }

        private static List<Placement> CreatePlacements(int width, int height, int shipLength)
        {
            int horizontalCount = Math.Max(0, width - shipLength + 1) * height;
            int verticalCount = shipLength == 1 ? 0 : Math.Max(0, height - shipLength + 1) * width;
            var placements = new List<Placement>(horizontalCount + verticalCount);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x <= width - shipLength; x++)
                {
                    placements.Add(new Placement(y * width + x, 1));
                }
            }

            if (shipLength == 1)
            {
                return placements;
            }

            for (int y = 0; y <= height - shipLength; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    placements.Add(new Placement(y * width + x, width));
                }
            }

            return placements;
        }

        private static bool CanPlace(Placement placement, int shipLength, sbyte[] shipIndicesByCell)
        {
            int cellIndex = placement.startCellIndex;

            for (int shipCellIndex = 0; shipCellIndex < shipLength; shipCellIndex++)
            {
                if (shipIndicesByCell[cellIndex] != EMPTY_CELL)
                {
                    return false;
                }

                cellIndex += placement.step;
            }

            return true;
        }

        private static void SetPlacement(
            Placement placement,
            int shipLength,
            sbyte shipIndex,
            sbyte[] shipIndicesByCell)
        {
            int cellIndex = placement.startCellIndex;

            for (int shipCellIndex = 0; shipCellIndex < shipLength; shipCellIndex++)
            {
                shipIndicesByCell[cellIndex] = shipIndex;
                cellIndex += placement.step;
            }
        }

        private static void Shuffle(List<Placement> placements, ref DeterministicRandom random)
        {
            for (int index = placements.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                (placements[index], placements[swapIndex]) = (placements[swapIndex], placements[index]);
            }
        }

        private readonly struct Placement
        {
            public int startCellIndex { get; }
            public int step { get; }

            public Placement(int startCellIndex, int step)
            {
                this.startCellIndex = startCellIndex;
                this.step = step;
            }
        }

        private struct DeterministicRandom
        {
            private uint _state;

            public DeterministicRandom(int seed)
            {
                _state = seed == 0 ? 0x6D2B79F5u : unchecked((uint)seed);
            }

            public int Next(int maximumExclusive)
            {
                _state ^= _state << 13;
                _state ^= _state >> 17;
                _state ^= _state << 5;

                return (int)(_state % (uint)maximumExclusive);
            }
        }
    }
}
