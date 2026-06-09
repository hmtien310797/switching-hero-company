using System;
using RecyclableScrollRect;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageNodeItemView : BaseItem
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedArrow;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject currentStageMark;

        private int stage;
        private Action<int> onClick;

        public int Stage => stage;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        public void Bind(
            int stage,
            Sprite stageIcon,
            bool isSelected,
            bool isLocked,
            bool isCurrentStage,
            Action<int> onClick
        )
        {
            this.stage = stage;
            this.onClick = onClick;

            if (stageText != null)
                stageText.text = stage.ToString();

            if (icon != null)
            {
                icon.sprite = stageIcon;
                icon.gameObject.SetActive(stageIcon != null);
            }

            if (selectedArrow != null)
                selectedArrow.SetActive(isSelected);

            if (lockOverlay != null)
                lockOverlay.SetActive(isLocked);

            if (currentStageMark != null)
                currentStageMark.SetActive(isCurrentStage);

            if (button != null)
                button.interactable = !isLocked;
        }

        private void HandleClick()
        {
            onClick?.Invoke(stage);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}