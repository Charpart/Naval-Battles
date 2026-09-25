using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Domain.Fleets;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Domain.Boards
{
    [TestFixture]
    public sealed class BoardTests
    {
        [Test]
        public void TryShoot_WhenCellIsEmpty_ReturnsMiss()
        {
            // Arrange
            Board board = CreateBoard();
            int emptyCellIndex = FindCell(board, false);

            // Act
            bool wasAccepted = board.TryShoot(emptyCellIndex, out ShotOutcome outcome);

            // Assert
            Assert.That(wasAccepted, Is.True);
            Assert.That(outcome.result, Is.EqualTo(ShotResult.Miss));
            Assert.That(board.GetCellShotState(emptyCellIndex), Is.EqualTo(CellShotState.Miss));
        }

        [Test]
        public void TryShoot_WhenSingleCellShipIsHit_ReturnsSunk()
        {
            // Arrange
            Board board = CreateBoard();
            int shipIndex = FindShipByLength(board, 1);
            int cellIndex = FindCellForShip(board, shipIndex);

            // Act
            bool wasAccepted = board.TryShoot(cellIndex, out ShotOutcome outcome);

            // Assert
            Assert.That(wasAccepted, Is.True);
            Assert.That(outcome.result, Is.EqualTo(ShotResult.Sunk));
            Assert.That(outcome.shipIndex, Is.EqualTo(shipIndex));
            Assert.That(board.IsShipSunk(shipIndex), Is.True);
        }

        [Test]
        public void TryShoot_WhenCellWasAlreadyShot_ReturnsFalse()
        {
            // Arrange
            Board board = CreateBoard();
            int cellIndex = FindCell(board, false);
            board.TryShoot(cellIndex, out ShotOutcome firstOutcome);

            // Act
            bool wasAccepted = board.TryShoot(cellIndex, out ShotOutcome secondOutcome);

            // Assert
            Assert.That(firstOutcome.result, Is.EqualTo(ShotResult.Miss));
            Assert.That(wasAccepted, Is.False);
            Assert.That(secondOutcome.result, Is.EqualTo(ShotResult.Invalid));
        }

        [TestCase(-1)]
        [TestCase(36)]
        public void TryShoot_WhenCellIsOutsideBoard_ReturnsFalse(int cellIndex)
        {
            // Arrange
            Board board = CreateBoard();

            // Act
            bool wasAccepted = board.TryShoot(cellIndex, out ShotOutcome outcome);

            // Assert
            Assert.That(wasAccepted, Is.False);
            Assert.That(outcome.result, Is.EqualTo(ShotResult.Invalid));
        }

        private Board CreateBoard()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            GameRules.TryCreate(6, 6, shipLengths, 15.0, out GameRules rules, out GameRulesValidationError error);
            bool wasCreated = FleetPlacementSystem.TryCreateBoard(rules, 7345, out Board board);

            Assert.That(error, Is.EqualTo(GameRulesValidationError.None));
            Assert.That(wasCreated, Is.True);

            return board;
        }

        private int FindCell(Board board, bool hasShip)
        {
            for (int cellIndex = 0; cellIndex < board.totalCellCount; cellIndex++)
            {
                if ((board.GetShipIndex(cellIndex) >= 0) == hasShip)
                {
                    return cellIndex;
                }
            }

            return -1;
        }

        private int FindShipByLength(Board board, int shipLength)
        {
            for (int shipIndex = 0; shipIndex < board.shipCount; shipIndex++)
            {
                if (board.GetShipLength(shipIndex) == shipLength)
                {
                    return shipIndex;
                }
            }

            return -1;
        }

        private int FindCellForShip(Board board, int expectedShipIndex)
        {
            for (int cellIndex = 0; cellIndex < board.totalCellCount; cellIndex++)
            {
                if (board.GetShipIndex(cellIndex) == expectedShipIndex)
                {
                    return cellIndex;
                }
            }

            return -1;
        }
    }
}
