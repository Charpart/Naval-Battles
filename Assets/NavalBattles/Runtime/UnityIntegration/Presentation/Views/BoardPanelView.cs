using System;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class BoardPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private BoardView _board;
        [SerializeField] private FleetView _fleet;

        public event Action<int> cellSelected;

        private void Awake()
        {
            _board.cellSelected += OnCellSelected;
        }

        private void OnDestroy()
        {
            _board.cellSelected -= OnCellSelected;
        }

        public void Render(BoardPanelModel model)
        {
            _titleLabel.text = model.title;
            _board.Render(model);
            _fleet.Render(model.shipGroups);
        }

        public void SetInteractive(bool isInteractive)
        {
            _board.SetInteractive(isInteractive);
        }

        private void OnCellSelected(int cellIndex)
        {
            cellSelected?.Invoke(cellIndex);
        }
    }
}
