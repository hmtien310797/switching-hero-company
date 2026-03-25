using System.Collections;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.HeroUIView;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonSequencePopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private HeroSummonSequenceCardUI cardPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button skipRevealButton;
        [SerializeField] private HeroSummonRarityVisualConfigSO rarityVisualConfig;

        [Header("Auto Summon")] [SerializeField]
        private HeroSummonAutoSummonController autoSummonController;

        [SerializeField] private float autoSummonDelay = 0.4f;

        [Header("Reveal")] [SerializeField] private float initialDelay = 0.15f;
        [SerializeField] private float revealInterval = 0.2f;

        private readonly List<HeroSummonSequenceCardUI> spawnedCards = new();
        private Coroutine revealCoroutine;
        private Coroutine autoSummonCoroutine;

        private HeroSummonResult currentResult;
        private System.Action autoSummonAction;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (skipRevealButton != null)
                skipRevealButton.onClick.AddListener(SkipReveal);

            Hide();
        }

        public void Show(HeroSummonResult result, System.Action onAutoSummon = null)
        {
            if (result == null) return;

            currentResult = result;
            autoSummonAction = onAutoSummon;

            ClearCards();

            var groupedEntries = HeroSummonResultGrouper.Group(result);

            for (int i = 0; i < groupedEntries.Count; i++)
            {
                var entry = groupedEntries[i];
                var visual = rarityVisualConfig != null ? rarityVisualConfig.Get(entry.Rarity) : null;

                var card = Instantiate(cardPrefab, cardRoot);
                card.Bind(
                    entry,
                    visual != null ? visual.Icon : null,
                    visual != null ? visual.TopColor : Color.white,
                    visual != null ? visual.BottomColor : Color.white);

                spawnedCards.Add(card);
            }

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            if (revealCoroutine != null)
                StopCoroutine(revealCoroutine);

            if (autoSummonCoroutine != null)
                StopCoroutine(autoSummonCoroutine);

            bool skipAnim = autoSummonController != null && autoSummonController.IsSkipAnimationOn;
            if (skipAnim)
            {
                SkipReveal();

                if (autoSummonController != null && autoSummonController.IsAutoSummonOn)
                    autoSummonCoroutine = StartCoroutine(CoAutoSummon());
            }
            else
            {
                revealCoroutine = StartCoroutine(CoRevealCards());
            }
        }

        public void Hide()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            if (autoSummonCoroutine != null)
            {
                StopCoroutine(autoSummonCoroutine);
                autoSummonCoroutine = null;
            }

            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);

            ClearCards();
            currentResult = null;
            autoSummonAction = null;
        }

        private IEnumerator CoRevealCards()
        {
            yield return new WaitForSeconds(initialDelay);

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    spawnedCards[i].Reveal();

                yield return new WaitForSeconds(revealInterval);
            }

            revealCoroutine = null;

            if (autoSummonController != null && autoSummonController.IsAutoSummonOn)
                autoSummonCoroutine = StartCoroutine(CoAutoSummon());
        }

        private IEnumerator CoAutoSummon()
        {
            yield return new WaitForSeconds(autoSummonDelay);

            Hide();
            autoSummonAction?.Invoke();

            autoSummonCoroutine = null;
        }

        private void SkipReveal()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    spawnedCards[i].ShowImmediate();
            }
        }

        private void ClearCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i].gameObject);
            }

            spawnedCards.Clear();
        }
    }
}