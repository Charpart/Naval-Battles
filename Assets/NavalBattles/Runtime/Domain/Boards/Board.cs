using System.Linq;
using NavalBattles.Runtime.Domain.Fleets;

namespace NavalBattles.Runtime.Domain.Boards
{
    public sealed class Board
    {
        private const sbyte NO_SHIP = -1;

        private readonly sbyte[] _shipIndexByCell;
        private readonly CellShotState[] _shots;
        private readonly ShipState[] _ships;

        public int width { get; }
        public int height { get; }

        public int totalCellCount => _shipIndexByCell.Length;
        public int shipCount => _ships.Length;
        public int shipCellCount { get; }

        public Board(int width, int height, sbyte[] shipIndexByCell, ShipState[] ships)
        {
            this.width = width;
            this.height = height;
            _shipIndexByCell = shipIndexByCell;
            _ships = ships;
            _shots = new CellShotState[shipIndexByCell.Length];

            int occupiedCells = shipIndexByCell.Count(t => t != NO_SHIP);
            shipCellCount = occupiedCells;
        }

        public int GetShipIndex(int cellIndex)
        {
            return IsCellIndexValid(cellIndex) == false ? NO_SHIP : _shipIndexByCell[cellIndex];
        }

        public int GetShipLength(int shipIndex)
        {
            return IsShipIndexValid(shipIndex) == false ? 0 : _ships[shipIndex].length;
        }

        public CellShotState GetCellShotState(int cellIndex)
        {
            return IsCellIndexValid(cellIndex) == false ? CellShotState.None : _shots[cellIndex];
        }

        public bool IsShipSunk(int shipIndex)
        {
            return IsShipIndexValid(shipIndex) && _ships[shipIndex].isSunk;
        }

        public bool AreAllShipsSunk()
        {
            return _ships.All(t => t.isSunk);
        }

        public bool TryShoot(int cellIndex, out ShotOutcome outcome)
        {
            outcome = ShotOutcome.Invalid;

            if (IsCellIndexValid(cellIndex) == false || _shots[cellIndex] != CellShotState.None)
            {
                return false;
            }

            int shipIndex = _shipIndexByCell[cellIndex];
            if (shipIndex == NO_SHIP)
            {
                _shots[cellIndex] = CellShotState.Miss;
                outcome = new ShotOutcome(ShotResult.Miss, NO_SHIP, 0);
                return true;
            }

            _shots[cellIndex] = CellShotState.Hit;
            ShipState updatedShip = _ships[shipIndex].RegisterHit();
            _ships[shipIndex] = updatedShip;
            ShotResult result = updatedShip.isSunk ? ShotResult.Sunk : ShotResult.Hit;
            outcome = new ShotOutcome(result, shipIndex, updatedShip.length);

            return true;
        }

        private bool IsCellIndexValid(int cellIndex)
        {
            return cellIndex >= 0 && cellIndex < _shipIndexByCell.Length;
        }

        private bool IsShipIndexValid(int shipIndex)
        {
            return shipIndex >= 0 && shipIndex < _ships.Length;
        }
    }
}
