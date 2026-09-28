using System.Collections.Generic;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class FleetItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform _deckContainer;
        [SerializeField] private Image _deckPrefab;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private float _deckSpacing = 2f;
        [SerializeField] private float _labelSpacing = 3f;

        private readonly List<Image> _decks = new List<Image>();
        private int _length;

        public void Render(ShipGroupModel model)
        {
            EnsureDecks(model.length);
            _countLabel.text = $"× {model.remainingCount}";
            LayoutContent();
            gameObject.SetActive(model.remainingCount > 0);
        }

        private void EnsureDecks(int length)
        {
            if (_length == length && _decks.Count == length)
                return;

            for (int index = 0; index < _decks.Count; index++)
            {
                Destroy(_decks[index].gameObject);
            }

            _decks.Clear();
            _length = length;

            for (int index = 0; index < length; index++)
            {
                _decks.Add(Instantiate(_deckPrefab, _deckContainer));
            }
        }

        private void LayoutContent()
        {
            float x = 0f;
            float deckHeight = 0f;

            for (int index = 0; index < _decks.Count; index++)
            {
                RectTransform deck = _decks[index].rectTransform;
                deck.anchorMin = new Vector2(0f, 0.5f);
                deck.anchorMax = new Vector2(0f, 0.5f);
                deck.pivot = new Vector2(0f, 0.5f);
                deck.anchoredPosition = new Vector2(x, 0f);

                x += deck.rect.width;
                deckHeight = Mathf.Max(deckHeight, deck.rect.height);

                if (index < _decks.Count - 1)
                    x += _deckSpacing;
            }

            _deckContainer.anchorMin = new Vector2(0f, 0.5f);
            _deckContainer.anchorMax = new Vector2(0f, 0.5f);
            _deckContainer.pivot = new Vector2(0f, 0.5f);
            _deckContainer.anchoredPosition = Vector2.zero;
            _deckContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x);
            _deckContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, deckHeight);

            RectTransform label = _countLabel.rectTransform;
            label.anchorMin = new Vector2(0f, 0.5f);
            label.anchorMax = new Vector2(0f, 0.5f);
            label.pivot = new Vector2(0f, 0.5f);
            label.anchoredPosition = new Vector2(x + (_decks.Count > 0 ? _labelSpacing : 0f), 0f);

            var rectTransform = (RectTransform)transform;
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                label.anchoredPosition.x + label.rect.width);
            rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(deckHeight, label.rect.height));
        }
    }
}
