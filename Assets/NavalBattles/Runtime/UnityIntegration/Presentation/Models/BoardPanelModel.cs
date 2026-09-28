using System;
using NavalBattles.Runtime.Protocol.Messages;
using NavalBattles.Runtime.Protocol.Snapshots;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Models
{
    public sealed class BoardPanelModel
    {
        private readonly int[] _shipLengths;

        public NetworkPlayerSlot owner { get; internal set; }
        public string title { get; internal set; }
        public int width { get; }
        public int height { get; }
        public CellVisualState[] cells { get; }
        public ShipGroupModel[] shipGroups { get; }
        public bool isInteractive { get; internal set; }

        public int shipCount => _shipLengths.Length;

        public BoardPanelModel(GameDefinition definition)
        {
            definition = definition ?? throw new ArgumentNullException(nameof(definition));
            width = definition.width;
            height = definition.height;
            
            if (definition.width <= 0 ||
                definition.height <= 0 ||
                (long)definition.width * definition.height > int.MaxValue)
            {
                throw new ArgumentException("Invalid board dimensions.", nameof(definition));
            }
            
            cells = new CellVisualState[width * height];
            _shipLengths = new int[definition.shipLengths.Count];

            for (int index = 0; index < _shipLengths.Length; index++)
            {
                _shipLengths[index] = definition.shipLengths[index];
            }

            shipGroups = CreateShipGroups(_shipLengths);
        }

        internal int GetShipLength(int shipIndex)
        {
            return _shipLengths[shipIndex];
        }

        private ShipGroupModel[] CreateShipGroups(int[] shipLengths)
        {
            var groups = new ShipGroupModel[shipLengths.Length];
            int groupCount = 0;

            for (int shipIndex = 0; shipIndex < shipLengths.Length; shipIndex++)
            {
                int length = shipLengths[shipIndex];
                if (ContainsLength(groups, groupCount, length) == false)
                    groups[groupCount++] = new ShipGroupModel(length);
            }

            Array.Resize(ref groups, groupCount);
            return groups;
        }

        private bool ContainsLength(ShipGroupModel[] groups, int count, int length)
        {
            for (int index = 0; index < count; index++)
            {
                if (groups[index].length == length)
                    return true;
            }
            return false;
        }
    }
}
