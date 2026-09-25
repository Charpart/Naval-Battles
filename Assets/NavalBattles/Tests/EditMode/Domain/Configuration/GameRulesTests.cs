using System.Collections.Generic;
using NavalBattles.Runtime.Domain.Configuration;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Domain.Configuration
{
    [TestFixture]
    public sealed class GameRulesTests
    {
        [Test]
        public void TryCreate_WithValidValues_CopiesConfiguration()
        {
            // Arrange
            int[] shipLengths = { 3, 2, 2, 1 };

            // Act
            bool wasCreated = GameRules.TryCreate(
                6,
                6,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);
            shipLengths[0] = 1;

            // Assert
            Assert.That(wasCreated, Is.True);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.None));
            Assert.That(rules.width, Is.EqualTo(6));
            Assert.That(rules.height, Is.EqualTo(6));
            Assert.That(rules.cellCount, Is.EqualTo(36));
            Assert.That(rules.turnDurationSeconds, Is.EqualTo(15.0));
            Assert.That(rules.shipLengths, Is.EqualTo(new[] { 3, 2, 2, 1 }));
        }

        [TestCase(0, 6)]
        [TestCase(6, 0)]
        [TestCase(-1, 6)]
        public void TryCreate_WithNonPositiveBoardSize_ReturnsInvalidBoardSize(int width, int height)
        {
            // Arrange
            int[] shipLengths = { 1 };

            // Act
            bool wasCreated = GameRules.TryCreate(
                width,
                height,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);

            // Assert
            Assert.That(wasCreated, Is.False);
            Assert.That(rules, Is.Null);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.InvalidBoardSize));
        }

        [Test]
        public void TryCreate_WithShipLongerThanBoard_ReturnsInvalidShipLength()
        {
            // Arrange
            int[] shipLengths = { 7 };

            // Act
            bool wasCreated = GameRules.TryCreate(
                6,
                6,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);

            // Assert
            Assert.That(wasCreated, Is.False);
            Assert.That(rules, Is.Null);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.InvalidShipLength));
        }

        [Test]
        public void TryCreate_WithFleetLargerThanBoard_ReturnsFleetDoesNotFit()
        {
            // Arrange
            int[] shipLengths = { 2, 2, 1 };

            // Act
            bool wasCreated = GameRules.TryCreate(
                2,
                2,
                shipLengths,
                15.0,
                out GameRules rules,
                out GameRulesValidationError error);

            // Assert
            Assert.That(wasCreated, Is.False);
            Assert.That(rules, Is.Null);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.FleetDoesNotFit));
        }

        [Test]
        public void TryCreate_WithInvalidTurnDuration_ReturnsInvalidTurnDuration()
        {
            // Arrange
            IReadOnlyList<int> shipLengths = new[] { 1 };

            // Act
            bool wasCreated = GameRules.TryCreate(
                2,
                2,
                shipLengths,
                0.0,
                out GameRules rules,
                out GameRulesValidationError error);

            // Assert
            Assert.That(wasCreated, Is.False);
            Assert.That(rules, Is.Null);
            Assert.That(error, Is.EqualTo(GameRulesValidationError.InvalidTurnDuration));
        }
    }
}
