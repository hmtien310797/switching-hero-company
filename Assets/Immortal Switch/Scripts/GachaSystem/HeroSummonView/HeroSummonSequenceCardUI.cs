using System.Collections;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonSequenceCardUI : MonoBehaviour
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private GameObject newTag;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Background")]
        [SerializeField] private UIGradient backgroundGradient;

        [Header("Reveal Anim")]
        [SerializeField] private float revealDuration = 0.22f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.72f, 0.72f, 1f);
        [SerializeField] private Vector3 shownScale = Vector3.one;

        [Header("Rare Feel")]
        [SerializeField] private float epicScaleMultiplier = 1.06f;
        [SerializeField] private float legendaryScaleMultiplier = 1.12f;
        [SerializeField] private float rarePulseDuration = 0.12f;

        private Coroutine revealCoroutine;
        private SummonRarity boundRarity;
        private bool isLastCard;

        public CanvasGroup CanvasGroup => canvasGroup;

        public void Bind(
            HeroSummonGroupedResultEntry entry,
            Sprite raritySprite,
            Color topColor,
            Color bottomColor,
            bool lastCard = false)
        {
            var hero = entry.HeroAsset as HeroDataSO;
            boundRarity = entry.Rarity;
            isLastCard = lastCard;

            if (portraitImage != null)
                portraitImage.sprite = hero != null ? hero.PortraitIcon : null;

            if (rarityIcon != null)
                rarityIcon.sprite = raritySprite;

            if (amountText != null)
                amountText.text = $"x{entry.Count}";

            if (heroNameText != null)
                heroNameText.text = entry.HeroName;

            if (newTag != null)
                newTag.SetActive(entry.IsNewHero);

            if (backgroundGradient != null)
            {
                backgroundGradient.TopColor = topColor;
                backgroundGradient.BottomColor = bottomColor;
                backgroundGradient.Refresh();
            }

            SetHiddenImmediate();
        }

        public void PrepareForReuse()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.localScale = hiddenScale;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (portraitImage != null)
                portraitImage.sprite = null;

            if (rarityIcon != null)
                rarityIcon.sprite = null;

            if (amountText != null)
                amountText.text = string.Empty;

            if (heroNameText != null)
                heroNameText.text = string.Empty;

            if (newTag != null)
                newTag.SetActive(false);

            boundRarity = SummonRarity.Common;
            isLastCard = false;
        }

        public void SetVisible(bool value)
        {
            gameObject.SetActive(value);
        }

        public void Reveal()
        {
            if (revealCoroutine != null)
                StopCoroutine(revealCoroutine);

            revealCoroutine = StartCoroutine(CoReveal());
        }

        public void ShowImmediate()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (rectTransform != null)
                rectTransform.localScale = GetFinalScale();
        }

        private Vector3 GetFinalScale()
        {
            float multiplier = 1f;

            if (boundRarity >= SummonRarity.Legendary)
                multiplier = legendaryScaleMultiplier;
            else if (boundRarity >= SummonRarity.Epic)
                multiplier = epicScaleMultiplier;

            if (isLastCard)
                multiplier += 0.03f;

            return shownScale * multiplier;
        }

        private void SetHiddenImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (rectTransform != null)
                rectTransform.localScale = hiddenScale;
        }

        private IEnumerator CoReveal()
        {
            Vector3 targetScale = GetFinalScale();
            Vector3 overshootScale = targetScale * (boundRarity >= SummonRarity.Epic ? 1.08f : 1.04f);

            float time = 0f;
            while (time < revealDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / revealDuration);
                float eased = EaseOutBackStrong(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = t;

                if (rectTransform != null)
                    rectTransform.localScale = Vector3.LerpUnclamped(hiddenScale, overshootScale, eased);

                yield return null;
            }

            if (rectTransform != null)
                rectTransform.localScale = targetScale;

            if (boundRarity >= SummonRarity.Epic)
                yield return StartCoroutine(CoRarePulse(targetScale));

            ShowImmediate();
            revealCoroutine = null;
        }

        private IEnumerator CoRarePulse(Vector3 baseScale)
        {
            Vector3 pulseScale = baseScale * 1.04f;
            float time = 0f;

            while (time < rarePulseDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / rarePulseDuration);
                if (rectTransform != null)
                    rectTransform.localScale = Vector3.Lerp(baseScale, pulseScale, t);
                yield return null;
            }

            time = 0f;
            while (time < rarePulseDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / rarePulseDuration);
                if (rectTransform != null)
                    rectTransform.localScale = Vector3.Lerp(pulseScale, baseScale, t);
                yield return null;
            }
        }

        private float EaseOutBackStrong(float t)
        {
            const float c1 = 2.2f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}