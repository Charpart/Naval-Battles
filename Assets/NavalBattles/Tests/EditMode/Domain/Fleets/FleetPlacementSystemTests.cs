using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Domain.Fleets;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Domain.Fleets
{
    [TestFixture]
    public sealed class FleetPlacementSystemTests
    {
        private const int BOARD_SIZE = 6;
        private const int SEED = 7345;

        [Test]
        public void TryCreateBoard_WithSameSeed_CreatesSameLayout()
        {
            // Arrange
            GameRules rules = CreateRules();

            // Act
            bool wasFirstCreated = FleetPlacementSystem.TryCreateBoard(rules, SEED, out Board first);
            bool wasSecondCreated = FleetPlacementSystem.TryCreateBoard(rules, SEED, out Board second);

            // Assert
            Assert.That(wasFirstCreated, Is.True);
            Assert.That(wasSecondCreated, Is.True);

            for (int cellIndex = 0; cellIndex < rules.cellCount; cellIndex++)
            {
                Assert.That(second.GetShipIndex(cellIndex), Is.EqualTo(first.GetShipIndex(cellIndex)));
            }
        }

        [Test]
        public void TryCreateBoard_WithValidRules_PlacesExpectedFleetWithoutOverlap()
        {
            // Arrange
            GameRules rules = CreateRules();

            // Act
            bool wasCreated = FleetPlacementSystem.TryCreateBoard(rules, SEED, out Board board);

            // Assert
            Assert.That(wasCreated, Is.True);
            Assert.That(board.shipCount, Is.EqualTo(4));
            Assert.That(board.shipCellCount, Is.EqualTo(8));
            Assert.That(board.GetShipLength(0), Is.EqualTo(3));
            Assert.That(board.GetShipLength(1), Is.EqualTo(2));
            Assert.That(board.GetShipLength(2), Is.EqualTo(2));
            Assert.That(board.GetShipLength(3), Is.EqualTo(1));

            for (int cellIndex = 0; cellIndex < rules.cellCount; cellIndex++)
            {
                int shipIndex = board.GetShipIndex(cellIndex);
                Assert.That(shipIndex, Is.InRange(-1, board.shipCount - 1));
            }
        }

        [Test]
        public void TryCreateBoard_WithValidRules_LeavesWaterBetweenShips()
        {
            // Arrange
            GameRules rules = CreateRules();

            for (int seed = 1; seed <= 128; seed++)
            {
                // Act
                bool wasCreated = FleetPlacementSystem.TryCreateBoard(rules, seed, out Board board);

                // Assert
                Assert.That(wasCreated, Is.True);
                Assert.That(HasAdjacentShips(board), Is.False, $"Seed {seed} placed ships next to each other.");
            }
        }

        private GameRules CreateRules()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            bool wasCreated = GameRules.TryCreate(
                BOARD_SIZE,
                BOARD_SIZE,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);

            Assert.That(wasCreated, Is.True, error.ToString());
            return rules;
        }

        private bool HasAdjacentShips(Board board)
        {
            for (int cellIndex = 0; cellIndex < board.totalCellCount; cellIndex++)
            {
                int shipIndex = board.GetShipIndex(cellIndex);
                if (shipIndex < 0 || HasAdjacentShip(board, cellIndex, shipIndex) == false)
                    continue;

                return true;
            }
            return false;
        }

        private bool HasAdjacentShip(Board board, int cellIndex, int shipIndex)
        {
            int cellX = cellIndex % board.width;
            int cellY = cellIndex / board.width;

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int adjacentX = cellX + offsetX;
                    int adjacentY = cellY + offsetY;

                    if ((offsetX == 0 && offsetY == 0) ||
                        adjacentX < 0 || adjacentX >= board.width ||
                        adjacentY < 0 || adjacentY >= board.height)
                    {
                        continue;
                    }

                    int adjacentShipIndex = board.GetShipIndex(adjacentY * board.width + adjacentX);
                    if (adjacentShipIndex >= 0 && adjacentShipIndex != shipIndex)
                        return true;
                }
            }
            return false;
        }
    }
}
