using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Domain.Matches;
using NUnit.Framework;

namespace NavalBattles.Tests.EditMode.Domain.Matches
{
    [TestFixture]
    public sealed class MatchSystemTests
    {
        private const double TURN_DURATION_SECONDS = 15.0;

        [Test]
        public void TryCreate_WithSameSeed_CreatesSameStartingPlayerAndBoards()
        {
            // Arrange
            GameRules rules = CreateDefaultRules();

            // Act
            bool wasFirstCreated = MatchSystem.TryCreate(rules, 42, 100.0, out MatchState first);
            bool wasSecondCreated = MatchSystem.TryCreate(rules, 42, 100.0, out MatchState second);

            // Assert
            Assert.That(wasFirstCreated, Is.True);
            Assert.That(wasSecondCreated, Is.True);
            Assert.That(second.activePlayer, Is.EqualTo(first.activePlayer));
            Assert.That(second.turnId, Is.EqualTo(first.turnId));
            Assert.That(second.turnDeadline, Is.EqualTo(first.turnDeadline));

            for (int cellIndex = 0; cellIndex < rules.cellCount; cellIndex++)
            {
                Assert.That(
                    second.GetBoard(PlayerSlot.First).GetShipIndex(cellIndex),
                    Is.EqualTo(first.GetBoard(PlayerSlot.First).GetShipIndex(cellIndex)));
                Assert.That(
                    second.GetBoard(PlayerSlot.Second).GetShipIndex(cellIndex),
                    Is.EqualTo(first.GetBoard(PlayerSlot.Second).GetShipIndex(cellIndex)));
            }
        }

        [Test]
        public void TryFire_WhenShotIsAccepted_SwitchesTurnEvenOnHit()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            PlayerSlot shooter = match.activePlayer;
            Board opponentBoard = match.GetBoard(PlayerSlotUtility.GetOpponent(shooter));
            int targetCellIndex = FindCell(opponentBoard, true);
            ulong initialTurnId = match.turnId;
            ulong initialRevision = match.revision;

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                shooter,
                initialTurnId,
                targetCellIndex,
                101.0,
                out FireDecision decision);

            // Assert
            Assert.That(wasAccepted, Is.True);
            Assert.That(decision.status, Is.EqualTo(FireDecisionStatus.Accepted));
            Assert.That(
                decision.outcome.result,
                Is.EqualTo(ShotResult.Hit).Or.EqualTo(ShotResult.Sunk));
            Assert.That(match.activePlayer, Is.EqualTo(PlayerSlotUtility.GetOpponent(shooter)));
            Assert.That(match.turnId, Is.EqualTo(initialTurnId + 1));
            Assert.That(match.revision, Is.EqualTo(initialRevision + 1));
            Assert.That(match.turnDeadline, Is.EqualTo(101.0 + TURN_DURATION_SECONDS));
        }

        [Test]
        public void TryFire_WhenPlayerIsNotActive_ReturnsWrongTurnWithoutMutation()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            PlayerSlot inactivePlayer = PlayerSlotUtility.GetOpponent(match.activePlayer);
            ulong initialRevision = match.revision;

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                inactivePlayer,
                match.turnId,
                0,
                101.0,
                out FireDecision decision);

            // Assert
            Assert.That(wasAccepted, Is.False);
            Assert.That(decision.status, Is.EqualTo(FireDecisionStatus.WrongTurn));
            Assert.That(match.revision, Is.EqualTo(initialRevision));
        }

        [Test]
        public void TryFire_WithStaleTurnId_ReturnsStaleTurnWithoutMutation()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            ulong initialRevision = match.revision;

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                match.activePlayer,
                match.turnId + 1,
                0,
                101.0,
                out FireDecision decision);

            // Assert
            Assert.That(wasAccepted, Is.False);
            Assert.That(decision.status, Is.EqualTo(FireDecisionStatus.StaleTurn));
            Assert.That(match.revision, Is.EqualTo(initialRevision));
        }

        [Test]
        public void TryAdvanceTimeout_AtDeadline_SwitchesTurnExactlyOnce()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            PlayerSlot initialPlayer = match.activePlayer;
            double deadline = match.turnDeadline;

            // Act
            bool wasFirstAdvanced = MatchSystem.TryAdvanceTimeout(match, deadline);
            bool wasSecondAdvanced = MatchSystem.TryAdvanceTimeout(match, deadline);

            // Assert
            Assert.That(wasFirstAdvanced, Is.True);
            Assert.That(wasSecondAdvanced, Is.False);
            Assert.That(match.activePlayer, Is.EqualTo(PlayerSlotUtility.GetOpponent(initialPlayer)));
            Assert.That(match.turnId, Is.EqualTo(2));
            Assert.That(match.revision, Is.EqualTo(1));
        }

        [Test]
        public void TryFire_AfterTimeoutWithPreviousTurnId_ReturnsStaleTurn()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            PlayerSlot previousPlayer = match.activePlayer;
            ulong previousTurnId = match.turnId;
            double deadline = match.turnDeadline;
            MatchSystem.TryAdvanceTimeout(match, deadline);

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                previousPlayer,
                previousTurnId,
                0,
                deadline,
                out FireDecision decision);

            // Assert
            Assert.That(wasAccepted, Is.False);
            Assert.That(decision.status, Is.EqualTo(FireDecisionStatus.StaleTurn));
        }

        [Test]
        public void TryFire_ImmediatelyBeforeDeadline_AcceptsShot()
        {
            // Arrange
            MatchState match = CreateDefaultMatch();
            PlayerSlot shooter = match.activePlayer;
            double serverTime = match.turnDeadline - 0.001;

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                shooter,
                match.turnId,
                0,
                serverTime,
                out FireDecision decision);

            // Assert
            Assert.That(wasAccepted, Is.True);
            Assert.That(decision.status, Is.EqualTo(FireDecisionStatus.Accepted));
        }

        [Test]
        public void TryFire_WhenFinalShipIsSunk_SetsWinnerAndFinishesMatch()
        {
            // Arrange
            int[] shipLengths = { 1 };
            GameRules.TryCreate(2, 1, shipLengths, TURN_DURATION_SECONDS, out GameRules rules, out GameRulesValidationError error);
            MatchSystem.TryCreate(rules, 7, 100.0, out MatchState match);
            PlayerSlot shooter = match.activePlayer;
            Board opponentBoard = match.GetBoard(PlayerSlotUtility.GetOpponent(shooter));
            int targetCellIndex = FindCell(opponentBoard, true);

            // Act
            bool wasAccepted = MatchSystem.TryFire(
                match,
                shooter,
                match.turnId,
                targetCellIndex,
                101.0,
                out FireDecision decision);

            // Assert
            Assert.That(error, Is.EqualTo(GameRulesValidationError.None));
            Assert.That(wasAccepted, Is.True);
            Assert.That(decision.outcome.result, Is.EqualTo(ShotResult.Sunk));
            Assert.That(match.isFinished, Is.True);
            Assert.That(match.winner, Is.EqualTo(shooter));
            Assert.That(MatchSystem.TryAdvanceTimeout(match, 1000.0), Is.False);
        }

        private static MatchState CreateDefaultMatch()
        {
            GameRules rules = CreateDefaultRules();
            bool wasCreated = MatchSystem.TryCreate(rules, 42, 100.0, out MatchState match);
            Assert.That(wasCreated, Is.True);

            return match;
        }

        private static GameRules CreateDefaultRules()
        {
            int[] shipLengths = { 3, 2, 2, 1 };
            bool wasCreated = GameRules.TryCreate(
                6,
                6,
                shipLengths,
                TURN_DURATION_SECONDS,
                out GameRules rules,
                out GameRulesValidationError error);
            Assert.That(wasCreated, Is.True, error.ToString());

            return rules;
        }

        private static int FindCell(Board board, bool hasShip)
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
    }
}
