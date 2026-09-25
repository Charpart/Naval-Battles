using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;
using NavalBattles.Runtime.Domain.Fleets;

namespace NavalBattles.Runtime.Domain.Matches
{
    public static class MatchSystem
    {
        private const int FIRST_BOARD_SEED_SALT = 0x2C9277B5;
        private const int SECOND_BOARD_SEED_SALT = 0x5F356495;

        public static bool TryCreate(GameRules rules, int seed, double serverTime, out MatchState match)
        {
            match = null;

            if (rules == null)
            {
                return false;
            }

            if (TryCreateBoards(rules, seed, out Board firstBoard, out Board secondBoard) == false)
            {
                return false;
            }

            PlayerSlot activePlayer = (seed & 1) == 0 ? PlayerSlot.First : PlayerSlot.Second;
            double turnDeadline = serverTime + rules.turnDurationSeconds;
            match = new MatchState(rules, firstBoard, secondBoard, activePlayer, turnDeadline);

            return true;
        }

        private static bool TryCreateBoards(
            GameRules rules,
            int seed,
            out Board firstBoard,
            out Board secondBoard)
        {
            secondBoard = null;

            return FleetPlacementSystem.TryCreateBoard(
                    rules,
                    seed ^ FIRST_BOARD_SEED_SALT,
                    out firstBoard) &&
                FleetPlacementSystem.TryCreateBoard(
                    rules,
                    seed ^ SECOND_BOARD_SEED_SALT,
                    out secondBoard);
        }

        public static bool TryFire(
            MatchState match,
            PlayerSlot player,
            ulong turnId,
            int cellIndex,
            double serverTime,
            out FireDecision decision)
        {
            decision = new FireDecision(FireDecisionStatus.None, ShotOutcome.Invalid);

            if (match == null || match.isFinished)
            {
                decision = new FireDecision(FireDecisionStatus.MatchFinished, ShotOutcome.Invalid);
                return false;
            }

            if (turnId != match.turnId)
            {
                decision = new FireDecision(FireDecisionStatus.StaleTurn, ShotOutcome.Invalid);
                return false;
            }

            if (player != match.activePlayer)
            {
                decision = new FireDecision(FireDecisionStatus.WrongTurn, ShotOutcome.Invalid);
                return false;
            }

            if (serverTime >= match.turnDeadline)
            {
                decision = new FireDecision(FireDecisionStatus.TurnExpired, ShotOutcome.Invalid);
                return false;
            }

            PlayerSlot opponent = PlayerSlotUtility.GetOpponent(player);
            Board opponentBoard = match.GetBoard(opponent);
            bool wasShotAccepted = opponentBoard.TryShoot(cellIndex, out ShotOutcome outcome);

            if (wasShotAccepted == false)
            {
                decision = new FireDecision(FireDecisionStatus.InvalidTarget, ShotOutcome.Invalid);
                return false;
            }

            CompleteCurrentTurn(match);

            if (opponentBoard.AreAllShipsSunk())
            {
                match.winner = player;
            }
            else
            {
                StartTurn(match, opponent, serverTime);
            }

            decision = new FireDecision(FireDecisionStatus.Accepted, outcome);

            return true;
        }

        public static bool TryAdvanceTimeout(MatchState match, double serverTime)
        {
            if (match == null || match.isFinished || serverTime < match.turnDeadline)
            {
                return false;
            }

            CompleteCurrentTurn(match);
            StartTurn(match, PlayerSlotUtility.GetOpponent(match.activePlayer), serverTime);

            return true;
        }

        private static void CompleteCurrentTurn(MatchState match)
        {
            match.turnId++;
            match.revision++;
        }

        private static void StartTurn(MatchState match, PlayerSlot player, double serverTime)
        {
            match.activePlayer = player;
            match.turnDeadline = serverTime + match.rules.turnDurationSeconds;
        }
    }
}
