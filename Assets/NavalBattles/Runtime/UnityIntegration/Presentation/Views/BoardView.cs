using System;
using System.Collections.Generic;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using UnityEngine;
using UnityEngine.UI;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private BoardCellView _cellPrefab;

        private readonly List<BoardCellView> _cells = new List<BoardCellView>();

        public event Action<int> cellSelected;

        public void Render(BoardPanelModel model)
        {
            EnsureCells(model.width, model.height);
            RenderCells(model.cells, model.isInteractive);
        }

        public void Render(
            int width,
            int height,
            IReadOnlyList<CellVisualState> states,
            bool isInteractive)
        {
            EnsureCells(width, height);
            RenderCells(states, isInteractive);
        }

        public void SetInteractive(bool isInteractive)
        {
            for (int index = 0; index < _cells.Count; index++)
            {
                _cells[index].Render(CellVisualState.Unknown, isInteractive);
            }
        }

        private void EnsureCells(int width, int height)
        {
            int requiredCellCount = width * height;
            if (_cells.Count == requiredCellCount)
                return;

            ClearCells();
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = width;

            for (int cellIndex = 0; cellIndex < requiredCellCount; cellIndex++)
            {
                BoardCellView cell = Instantiate(_cellPrefab, _grid.transform);
                cell.name = $"Cell {cellIndex}";
                cell.Initialize(cellIndex, OnCellSelected);
                _cells.Add(cell);
            }
        }

        private void RenderCells(IReadOnlyList<CellVisualState> states, bool isInteractive)
        {
            if (states.Count != _cells.Count)
                throw new ArgumentException("Cell states do not match the board size.", nameof(states));

            for (int cellIndex = 0; cellIndex < _cells.Count; cellIndex++)
            {
                _cells[cellIndex].Render(states[cellIndex], isInteractive);
            }
        }

        private void ClearCells()
        {
            for (int index = 0; index < _cells.Count; index++)
            {
                Destroy(_cells[index].gameObject);
            }

            _cells.Clear();
        }

        private void OnCellSelected(int cellIndex)
        {
            cellSelected?.Invoke(cellIndex);
        }
    }
}
