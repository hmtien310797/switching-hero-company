using System.Collections;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public class SkillSummonSequenceCardUI : MonoBehaviour
    {
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Image rarityIcon;
        [SerializeField] private Image bgImg;
        [SerializeField] private Image frameImg;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text skillNameText;
        [SerializeField] private GameObject newTag;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

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
        [SerializeField] private float epicScaleMultiplier = 1.06f;
        [SerializeField] private float legendaryScaleMultiplier = 1.12f;
        [SerializeField] private float mythicScaleMultiplier = 1.16f;
        [SerializeField] private float rarePulseDuration = 0.12f;

        private Coroutine revealCoroutine;
        private SkillSummonGrade boundGrade;
        private bool isLastCard;

        public CanvasGroup CanvasGroup => canvasGroup;

        public void Bind(
            SkillSummonGroupedResultEntry entry,
            ItemTierEntry tierInfo,
            bool lastCard = false)
        {
            boundGrade = entry.Grade;
            isLastCard = lastCard;

            if (skillIconImage != null)
                skillIconImage.sprite = entry.Icon;

            if (rarityIcon != null)
            {
                rarityIcon.sprite = tierInfo.tierIcon;
                rarityIcon.enabled = tierInfo.tierIcon != null;
            }

            if (bgImg != null)
                bgImg.sprite = tierInfo.background;

            if (frameImg != null)
                frameImg.sprite = tierInfo.border;
            
            if (amountText != null)
                amountText.text = $"x{entry.Count}";

            if (skillNameText != null)
                skillNameText.text = entry.SkillName;

            if (newTag != null)
                newTag.SetActive(entry.IsNewSkill);

            //ApplyGlowByGrade(entry.Grade, topColor, bottomColor);
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

            if (skillIconImage != null)
                skillIconImage.sprite = null;

            if (rarityIcon != null)
                rarityIcon.sprite = null;

            if (amountText != null)
                amountText.text = string.Empty;

            if (skillNameText != null)
                skillNameText.text = string.Empty;

            if (newTag != null)
                newTag.SetActive(false);

            ResetGlow();

            boundGrade = SkillSummonGrade.B;
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

        private void ApplyGlowByGrade(SkillSummonGrade grade, Color topColor, Color bottomColor)
        {
            bool showGlow = grade >= SkillSummonGrade.S;

            if (glowRoot != null)
                glowRoot.SetActive(showGlow);

            if (!showGlow)
                return;

            Color backColor = Color.Lerp(bottomColor, Color.white, 0.25f);
            Color ringColor = Color.Lerp(topColor, Color.white, 0.15f);

            backColor.a = 1f;
            ringColor.a = 1f;

            if (glowBack != null)
                glowBack.color = backColor;

            if (glowRing != null)
                glowRing.color = ringColor;

            if (grade >= SkillSummonGrade.SS)
            {
                glowBackPulse?.SetProfile(0.45f, 0.90f, 2.8f, 0.98f, 1.10f, 2.0f, false, 0f);
                glowRingPulse?.SetProfile(0.55f, 1.00f, 3.2f, 0.98f, 1.12f, 2.2f, false, 22f);
            }
            else
            {
                glowBackPulse?.SetProfile(0.28f, 0.65f, 2.0f, 0.98f, 1.05f, 1.6f, false, 0f);
                glowRingPulse?.SetProfile(0.35f, 0.75f, 2.2f, 0.99f, 1.06f, 1.7f, false, 0f);
            }

            glowBackPulse?.ResetVisual();
            glowRingPulse?.ResetVisual();
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

            if (boundGrade >= SkillSummonGrade.SS)
                multiplier = mythicScaleMultiplier;
            else if (boundGrade >= SkillSummonGrade.S)
                multiplier = epicScaleMultiplier;
            else if (boundGrade >= SkillSummonGrade.A)
                multiplier = legendaryScaleMultiplier;

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
            Vector3 overshootScale = targetScale * (boundGrade >= SkillSummonGrade.S ? 1.08f : 1.04f);

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

            if (boundGrade >= SkillSummonGrade.S)
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