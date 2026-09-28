using System;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class BoardCellView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _unknownColor = new Color(0.12f, 0.25f, 0.38f);
        [SerializeField] private Color _waterColor = new Color(0.18f, 0.36f, 0.55f);
        [SerializeField] private Color _shipColor = new Color(0.35f, 0.42f, 0.48f);
        [SerializeField] private Color _missColor = new Color(0.20f, 0.48f, 0.68f);
        [SerializeField] private Color _hitColor = new Color(0.80f, 0.28f, 0.20f);
        [SerializeField] private Color _sunkColor = new Color(0.35f, 0.08f, 0.08f);
        [SerializeField] private string _shipSymbol = "■";
        [SerializeField] private string _missSymbol = "•";
        [SerializeField] private string _hitSymbol = "×";
        [SerializeField] private string _sunkSymbol = "✕";

        private Action<int> _selected;
        private int _cellIndex;
        private bool _isInitialized;

        public void Initialize(int cellIndex, Action<int> selected)
        {
            if (_isInitialized)
                _button.onClick.RemoveListener(OnClicked);

            _cellIndex = cellIndex;
            _selected = selected ?? throw new ArgumentNullException(nameof(selected));
            _button.onClick.AddListener(OnClicked);
            _isInitialized = true;
        }

        public void Render(CellVisualState state, bool isBoardInteractive)
        {
            _button.interactable = isBoardInteractive && state == CellVisualState.Unknown;
            _background.color = GetColor(state);
            _label.text = GetSymbol(state);
        }

        private void OnDestroy()
        {
            if (_isInitialized)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _selected?.Invoke(_cellIndex);
        }

        private Color GetColor(CellVisualState state)
        {
            return state switch
            {
                CellVisualState.Water => _waterColor,
                CellVisualState.Ship => _shipColor,
                CellVisualState.Miss => _missColor,
                CellVisualState.Hit => _hitColor,
                CellVisualState.Sunk => _sunkColor,
                _ => _unknownColor
            };
        }

        private string GetSymbol(CellVisualState state)
        {
            return state switch
            {
                CellVisualState.Ship => _shipSymbol,
                CellVisualState.Miss => _missSymbol,
                CellVisualState.Hit => _hitSymbol,
                CellVisualState.Sunk => _sunkSymbol,
                _ => string.Empty
            };
        }
    }
}
