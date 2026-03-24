using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionItemUI : MonoBehaviour
    {
        [Header("Icons")]
        [SerializeField] private Image portraitIcon;
        [SerializeField] private Image shardIcon;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private Image elementIcon;
        [SerializeField] private Image heroClassIcon;

        [Header("Texts")]
        [SerializeField] private TMP_Text progressText;

        [Header("Progress")]
        [SerializeField] private Image progressFill;

        [Header("States")]
        [SerializeField] private GameObject acquiredGroup;
        [SerializeField] private GameObject notAcquiredGroup;
        [SerializeField] private GameObject grayscaleOverlay;
        [SerializeField] private GameObject selectedObject;
        
        [Header("Tier Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private UIGradient gradient;
        [SerializeField] private HeroTierVisualConfigSO tierVisualConfig;

        [Header("Star")]
        [SerializeField] private Transform starRoot;
        [SerializeField] private GameObject starPrefab;
        [SerializeField] private GameObject emptyStarPrefab;

        private HeroCollectionItemViewData currentData;

        public HeroCollectionItemViewData Data => currentData;

        public void Bind(HeroCollectionItemViewData data)
        {
            currentData = data;

            if (portraitIcon != null) portraitIcon.sprite = data.PortraitIcon;
            if (shardIcon != null) shardIcon.sprite = data.ShardIcon;
            if (rarityIcon != null) rarityIcon.sprite = data.RarityIcon;
            if (elementIcon != null) elementIcon.sprite = data.ElementIcon;
            if (heroClassIcon != null) heroClassIcon.sprite = data.HeroClassIcon;

            bool isAcquired = data.IsAcquired;

            if (acquiredGroup != null) acquiredGroup.SetActive(isAcquired);
            if (notAcquiredGroup != null) notAcquiredGroup.SetActive(!isAcquired);
            if (grayscaleOverlay != null) grayscaleOverlay.SetActive(!isAcquired);

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
            }
            else
            {
                if (progressText != null)
                    progressText.text = string.Empty;

                if (progressFill != null)
                    progressFill.fillAmount = 0f;
                
                RefreshStars(0, 0);
            }

            SetSelected(false);
            if (data.IsAcquired)
            {
                ApplyTierVisual(data);
            }
            else
            {
                if (gradient != null) gradient.enabled = false;
                backgroundImage.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedObject != null)
                selectedObject.SetActive(isSelected);
        }
        
        private void ApplyTierVisual(HeroCollectionItemViewData data)
        {
            if (tierVisualConfig == null || backgroundImage == null)
                return;

            var entry = tierVisualConfig.Get(data.DisplayTier);
            if (entry == null) return;

            if (tierVisualConfig.Mode == TierVisualMode.Gradient)
            {
                if (gradient != null)
                {
                    gradient.enabled = true;
                    gradient.TopColor = entry.TopColor;
                    gradient.BottomColor = entry.BottomColor;
                }

                backgroundImage.sprite = null;
                backgroundImage.color = Color.white;
            }
            else
            {
                if (gradient != null)
                    gradient.enabled = false;

                backgroundImage.sprite = entry.BackgroundSprite;
                backgroundImage.color = Color.white;
            }
        }
        
        private void RefreshStars(int current, int max)
        {
            if (starRoot == null) return;

            foreach (Transform child in starRoot)
                Destroy(child.gameObject);

            for (int i = 0; i < max; i++)
            {
                var prefab = i < current ? starPrefab : emptyStarPrefab;
                Instantiate(prefab, starRoot);
            }
        }
    }
}