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
        [SerializeField] private GameObject newTag;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        [Header("Background")]
        [SerializeField] private UIGradient backgroundGradient;

        [Header("Reveal Anim")]
        [SerializeField] private float revealDuration = 0.18f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.7f, 0.7f, 1f);
        [SerializeField] private Vector3 shownScale = Vector3.one;

        private Coroutine revealCoroutine;

        public void Bind(
            HeroSummonGroupedResultEntry entry,
            Sprite raritySprite,
            Color topColor,
            Color bottomColor)
        {
            var hero = entry.HeroAsset as HeroDataSO;

            if (portraitImage != null && hero != null)
                portraitImage.sprite = hero.PortraitIcon;

            if (rarityIcon != null)
                rarityIcon.sprite = raritySprite;

            if (amountText != null)
                amountText.text = $"x{entry.Count}";

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
                rectTransform.localScale = shownScale;
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
            float time = 0f;

            while (time < revealDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / revealDuration);
                float eased = EaseOutBack(t);

                if (canvasGroup != null)
                    canvasGroup.alpha = t;

                if (rectTransform != null)
                    rectTransform.localScale = Vector3.LerpUnclamped(hiddenScale, shownScale, eased);

                yield return null;
            }

            ShowImmediate();
            revealCoroutine = null;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}