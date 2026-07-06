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
        [SerializeField] private GameObject selectedStageArrow;
        [SerializeField] private GameObject openDoor;
        [SerializeField] private GameObject passedDoor;
        [SerializeField] private GameObject belowCurrentStageIndicator;

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
            bool isViewportBelowCurrentStage,
            Action<int> onClick
        )
        {
            this.stage = stage;
            this.onClick = onClick;

            if (stageText != null)
                stageText.text = stage.ToString();

            // stage chua mo
            if (isLocked)
            {
                openDoor.SetActive(false);
                passedDoor.SetActive(false);
            }
            // stage hien tai
            else if (isCurrentStage)
            {
                openDoor.SetActive(true);
                passedDoor.SetActive(false);
            }
            // stage da vuot qua
            else
            {
                openDoor.SetActive(false);
                passedDoor.SetActive(true);
            }

            if (button != null)
                button.interactable = !isLocked;
            
            if (selectedStageArrow != null)
                selectedStageArrow.SetActive(isSelected);
            
            belowCurrentStageIndicator.SetActive(isViewportBelowCurrentStage);
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