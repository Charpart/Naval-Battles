using NavalBattles.Runtime.Domain.Boards;

namespace NavalBattles.Runtime.Domain.Matches
{
    public readonly struct FireDecision
    {
        public FireDecisionStatus status { get; }
        public ShotOutcome outcome { get; }

        public FireDecision(FireDecisionStatus status, ShotOutcome outcome)
        {
            this.status = status;
            this.outcome = outcome;
        }
    }
}
