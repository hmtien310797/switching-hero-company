using System;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionItemUI : MonoBehaviour
    {
        [Header("Icons")] [SerializeField] private Image portraitIcon;
        [SerializeField] private Image shardIcon;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private Image elementIcon;
        [SerializeField] private Image heroClassIcon;
        [SerializeField] private Image bgImg;
        [SerializeField] private Image frameImg;

        [Header("Texts")] [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text txtSlot;

        [Header("Progress")] [SerializeField] private Image progressFill;

        [Header("States")] [SerializeField] private GameObject acquiredGroup;
        [SerializeField] private GameObject notAcquiredGroup;
        [SerializeField] private GameObject grayscaleOverlay;
        [SerializeField] private GameObject goSlotPanel;

        [Tooltip("Viền chọn tĩnh")] [SerializeField]
        private GameObject selectedObject;

        [Tooltip("Glow sáng nhẹ / pulse khi đã chọn đủ source + target")] [SerializeField]
        private GameObject readyHighlightObject;

        [Header("Interaction")] [SerializeField]
        private Button button;

        [Header("Star")] [SerializeField] private Transform starRoot;
        [SerializeField] private GameObject starPrefab;
        [SerializeField] private GameObject emptyStarPrefab;

        private HeroCollectionItemViewData currentData;
        private Action<HeroCollectionItemUI> onClick;

        public HeroCollectionItemViewData Data => currentData;

        public void Bind(HeroCollectionItemViewData data)
        {
            currentData = data;

            if (portraitIcon != null)
                portraitIcon.sprite = data.PortraitIcon;

            if (shardIcon != null)
                shardIcon.sprite = data.ShardIcon;

            if (rarityIcon != null)
                rarityIcon.sprite = data.RarityIcon;

            if (elementIcon != null)
                elementIcon.sprite = data.ElementIcon;

            if (heroClassIcon != null)
                heroClassIcon.sprite = data.HeroClassIcon;

            bgImg.sprite = data.BgIcon;
            frameImg.sprite = data.FrameIcon;

            bool isAcquired = data.IsAcquired;

            if (acquiredGroup != null)
                acquiredGroup.SetActive(isAcquired);

            if (notAcquiredGroup != null)
                notAcquiredGroup.SetActive(!isAcquired);

            if (grayscaleOverlay != null)
                grayscaleOverlay.SetActive(!isAcquired);

            if (isAcquired)
            {
                if (data.IsMaxNode)
                {
                    if (progressText != null)
                        progressText.text = "MAX";

                    if (progressFill != null)
                        progressFill.fillAmount = 1f;
                }
                else
                {
                    if (progressText != null)
                        progressText.text = $"{data.CurrentShard}/{data.RequiredShardToNext}";

                    if (progressFill != null)
                        progressFill.fillAmount = data.ProgressNormalized;
                }

                RefreshStars(data.CurrentStarInTier, data.MaxStarInTier);
                ApplyTierVisual(data);
            }
            else
            {
                if (progressText != null)
                    progressText.text = $"{data.CurrentShard}/{data.RequiredShardToNext}";

                if (progressFill != null)
                    progressFill.fillAmount = data.ProgressNormalized;

                RefreshStars(0, 0);

                /*if (backgroundImage != null)
                {
                    backgroundImage.sprite = null;
                    backgroundImage.color = new Color(0.3f, 0.3f, 0.3f);
                }*/
            }

            if (goSlotPanel != null)
            {
                goSlotPanel.SetActive(data.IsInLineup);

                if (data.IsInLineup)
                {
                    txtSlot.text = $"Slot {data.LineupIdx + 1}";
                }
            }

            SetSelected(false);
            SetReadyHighlight(false);
            SetDimmed(false);
            SetButtonInteractable(true);
        }

        public void SetClickCallback(Action<HeroCollectionItemUI> clickCallback)
        {
            onClick = clickCallback;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void ClearClickCallback()
        {
            onClick = null;

            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        private void HandleClick()
        {
            onClick?.Invoke(this);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedObject != null)
                selectedObject.SetActive(isSelected);
        }

        public void SetReadyHighlight(bool isReady)
        {
            if (readyHighlightObject != null)
                readyHighlightObject.SetActive(isReady);
        }

        public void SetButtonInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;

            /*if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
            }*/
        }

        public void SetDimmed(bool dimmed)
        {
            /*if (canvasGroup != null)
                canvasGroup.alpha = dimmed ? 0.45f : 1f;*/
        }

        private void ApplyTierVisual(HeroCollectionItemViewData data)
        {
            /*if (tierVisualConfig == null || backgroundImage == null)
                return;

            var entry = tierVisualConfig.Get(data.DisplayTier);
            if (entry == null) return;

            if (tierVisualConfig.Mode == TierVisualMode.Gradient)
            {
                backgroundImage.sprite = null;
            }
            else
            {
                backgroundImage.sprite = entry.BackgroundSprite;
            }*/
        }

        private void RefreshStars(int current, int max)
        {
            if (starRoot == null)
                return;

            foreach (Transform child in starRoot)
                Destroy(child.gameObject);

            for (int i = 0; i < max; i++)
            {
                var prefab = i < current ? starPrefab : emptyStarPrefab;

                if (prefab != null)
                    Instantiate(prefab, starRoot);
            }
        }
    }
}