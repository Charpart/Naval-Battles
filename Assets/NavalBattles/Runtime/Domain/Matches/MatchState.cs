using NavalBattles.Runtime.Domain.Boards;
using NavalBattles.Runtime.Domain.Configuration;

namespace NavalBattles.Runtime.Domain.Matches
{
    public sealed class MatchState
    {
        private readonly Board _firstBoard;
        private readonly Board _secondBoard;

        public GameRules rules { get; }
        
        public PlayerSlot activePlayer { get; set; }
        public PlayerSlot winner { get; set; }
        public ulong turnId { get; set; }
        public ulong revision { get; set; }
        public double turnDeadline { get; set; }

        public bool isFinished => winner != PlayerSlot.None;

        public MatchState(
            GameRules rules,
            Board firstBoard,
            Board secondBoard,
            PlayerSlot activePlayer,
            double turnDeadline)
        {
            this.rules = rules;
            this.activePlayer = activePlayer;
            this.turnDeadline = turnDeadline;
            _firstBoard = firstBoard;
            _secondBoard = secondBoard;
            winner = PlayerSlot.None;
            turnId = 1;
            revision = 0;
        }

        public Board GetBoard(PlayerSlot player)
        {
            return player switch
            {
                PlayerSlot.First => _firstBoard,
                PlayerSlot.Second => _secondBoard,
                _ => null
            };
        }
    }
}
