using System.Collections;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonSequenceCardUI : MonoBehaviour
    {
        [Header("Main")]
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Image tierIcon;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private GameObject newTag;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Star")]
        [SerializeField] private UIWeaponStarDisplay starDisplay;

        [Header("Tier Visual")]
        [SerializeField] private WeaponTierVisualConfigSO tierVisualConfig;

        [Header("Background")]
        [SerializeField] private Image tierBackground;

        [Header("Glow")]
        [SerializeField] private GameObject glowRoot;
        [SerializeField] private Image glowBack;
        [SerializeField] private Image glowRing;
        [SerializeField] private UISoftGlowPulse glowBackPulse;
        [SerializeField] private UISoftGlowPulse glowRingPulse;

        [Header("Reveal Anim")]
        [SerializeField] private float revealDuration = 0.22f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.72f, 0.72f, 1f);
        [SerializeField] private Vector3 shownScale = Vector3.one;

        [Header("Rare Feel")]
        [SerializeField] private float tierAScaleMultiplier = 1.06f;
        [SerializeField] private float tierSScaleMultiplier = 1.12f;
        [SerializeField] private float tierSSScaleMultiplier = 1.16f;
        [SerializeField] private float highStarBonusScale = 0.03f;
        [SerializeField] private float rarePulseDuration = 0.12f;

        private Coroutine revealCoroutine;
        private WeaponTier boundTier = WeaponTier.D;
        private int boundStar = 1;
        private bool isLastCard;

        public CanvasGroup CanvasGroup => canvasGroup;

        public void Bind(WeaponSummonGroupedResultEntry entry, bool lastCard = false)
        {
            if (entry == null)
            {
                PrepareForReuse();
                return;
            }

            boundTier = entry.Tier;
            boundStar = entry.Star;
            isLastCard = lastCard;

            if (weaponIconImage != null)
            {
                weaponIconImage.sprite = entry.Icon;
                weaponIconImage.enabled = entry.Icon != null;
            }

            if (amountText != null)
                amountText.text = $"x{entry.TotalShardGained}";

            if (newTag != null)
                newTag.SetActive(entry.IsNewWeapon);

            if (starDisplay != null)
                starDisplay.BindStandard(entry.Star);

            ApplyTierVisual(entry.Tier);

            ApplyGlowByTier(entry.Tier, entry.Star);
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

            if (weaponIconImage != null)
            {
                weaponIconImage.sprite = null;
                weaponIconImage.enabled = false;
            }

            if (tierIcon != null)
            {
                tierIcon.sprite = null;
                tierIcon.enabled = false;
            }

            if (tierBackground != null)
            {
                tierBackground.sprite = null;
                tierBackground.enabled = false;
            }

            amountText.text = string.Empty;
            
            if (newTag != null)
                newTag.SetActive(false);
            
            starDisplay.BindStandard(0);

            ResetGlow();

            boundTier = WeaponTier.D;
            boundStar = 1;
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

        private void ApplyTierVisual(WeaponTier tier)
        {
            var visual = tierVisualConfig != null ? tierVisualConfig.Get(tier) : null;

            if (tierIcon != null)
            {
                tierIcon.sprite = visual != null ? visual.TierLabelSprite : null;
                tierIcon.enabled = tierIcon.sprite != null;
            }

            if (tierBackground != null)
            {
                tierBackground.sprite = visual != null ? visual.TierBackgroundSprite : null;
                tierBackground.enabled = tierBackground.sprite != null;
            }
        }

        private void ApplyGlowByTier(WeaponTier tier, int star)
        {
            var visual = tierVisualConfig != null ? tierVisualConfig.Get(tier) : null;

            bool showGlow = tier >= WeaponTier.S || star >= 4;

            if (glowRoot != null)
                glowRoot.SetActive(showGlow);

            if (!showGlow)
                return;

            Color glowColor = visual.GlowColor;

            glowColor.a = 1f;

            if (glowBack != null)
                glowBack.color = glowColor;

            if (glowRing != null)
                glowRing.color = glowColor;
        }

        private void ResetGlow()
        {
            if (glowRoot != null)
                glowRoot.SetActive(false);

            if (glowBack != null)
                glowBack.color = Color.clear;

            if (glowRing != null)
                glowRing.color = Color.clear;

            glowBackPulse?.ResetVisual();
            glowRingPulse?.ResetVisual();
        }

        private Vector3 GetFinalScale()
        {
            float multiplier = 1f;

            if (boundTier >= WeaponTier.SS)
                multiplier = tierSSScaleMultiplier;
            else if (boundTier >= WeaponTier.S)
                multiplier = tierSScaleMultiplier;
            else if (boundTier >= WeaponTier.A)
                multiplier = tierAScaleMultiplier;

            if (boundStar >= 4)
                multiplier += highStarBonusScale;

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
            Vector3 overshootScale = targetScale * (IsRareFeel() ? 1.08f : 1.04f);

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

            if (IsRareFeel())
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

        private bool IsRareFeel()
        {
            return boundTier >= WeaponTier.S || boundStar >= 4;
        }

        private float EaseOutBackStrong(float t)
        {
            const float c1 = 2.2f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}