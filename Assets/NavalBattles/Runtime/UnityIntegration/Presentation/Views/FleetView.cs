using System.Collections.Generic;
using NavalBattles.Runtime.UnityIntegration.Presentation.Models;
using UnityEngine;

namespace NavalBattles.Runtime.UnityIntegration.Presentation.Views
{
    public sealed class FleetView : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private FleetItemView _itemPrefab;
        [SerializeField] private float _spacing = 8f;

        private readonly List<FleetItemView> _items = new List<FleetItemView>();

        public void Render(IReadOnlyList<ShipGroupModel> groups)
        {
            EnsureItems(groups.Count);

            for (int index = 0; index < groups.Count; index++)
            {
                _items[index].Render(groups[index]);
            }

            LayoutItems();
        }

        private void EnsureItems(int count)
        {
            while (_items.Count < count)
            {
                _items.Add(Instantiate(_itemPrefab, _container));
            }

            for (int index = count; index < _items.Count; index++)
            {
                _items[index].gameObject.SetActive(false);
            }
        }

        private void LayoutItems()
        {
            float y = 0f;

            for (int index = 0; index < _items.Count; index++)
            {
                FleetItemView item = _items[index];
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                var rectTransform = (RectTransform)item.transform;
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = new Vector2(0f, -y);

                y += rectTransform.rect.height + _spacing;
            }
        }
    }
}
