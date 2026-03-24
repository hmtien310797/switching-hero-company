using System;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionFilterButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedObject;

        private Action onClick;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Init(Action clickAction)
        {
            onClick = clickAction;
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedObject != null)
                selectedObject.SetActive(isSelected);
        }

        private void HandleClick()
        {
            onClick?.Invoke();
        }
    }
}