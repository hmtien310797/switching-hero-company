using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroSwitchSlotUI : MonoBehaviour
    {
        [SerializeField] private HeroCollectionItemUI heroItemUI;
        [SerializeField] private TMP_Text slotText;
        [SerializeField] private Button button;

        private int heroId;
        private Action<int> onClick;

        public int HeroId => heroId;
        public HeroCollectionItemUI HeroItemUI => heroItemUI;

        public void Bind(int slotIndex, HeroCollectionItemViewData data, Action<int> clickCallback)
        {
            heroId = data != null ? data.HeroId : 0;
            onClick = clickCallback;

            if (slotText != null)
                slotText.text = $"Slot {slotIndex}";

            if (heroItemUI != null && data != null)
            {
                heroItemUI.Bind(data);
                heroItemUI.ClearClickCallback();
                
                heroItemUI.SetButtonInteractable(false);
                heroItemUI.SetDimmed(false);
                heroItemUI.SetSelected(false);
                heroItemUI.SetReadyHighlight(false);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void SetSelected(bool selected)
        {
            if (heroItemUI != null)
                heroItemUI.SetSelected(selected);
        }

        public void SetReadyHighlight(bool ready)
        {
            if (heroItemUI != null)
                heroItemUI.SetReadyHighlight(ready);
        }

        private void HandleClick()
        {
            onClick?.Invoke(heroId);
        }
    }
}