using System;
using System.Collections;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public class SkillSummonSequencePopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private SkillSummonSequenceCardUI cardPrefab;

        [Header("Top / Utility")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button skipRevealButton;

        [Header("Bottom Summon Buttons")]
        [SerializeField] private SummonButtonUI summonButtonA;
        [SerializeField] private SummonButtonUI summonButtonB;
        [SerializeField] private string optionAId = "summon_10";
        [SerializeField] private string optionBId = "summon_50";

        [Header("Auto / Skip")]
        [SerializeField] private SummonAutoSummonController autoSummonController;
        [SerializeField] private float autoSummonDelay = 0.35f;

        [Header("Reveal")]
        [SerializeField] private float initialDelay = 0.12f;
        [SerializeField] private float minRevealInterval = 0.05f;
        [SerializeField] private float maxRevealInterval = 0.18f;
        [SerializeField] private float highRarityExtraPause = 0.18f;
        [SerializeField] private float lastCardExtraPause = 0.12f;

        [Header("Rarity Visual")]
        [SerializeField] private SkillSummonRarityVisualConfigSO rarityVisualConfig;

        private readonly List<SkillSummonSequenceCardUI> spawnedCards = new();
        private readonly List<SkillSummonSequenceCardUI> cardPool = new();
        private readonly List<SkillSummonGroupedResultEntry> currentGroupedEntries = new();

        private Coroutine revealCoroutine;
        private Coroutine autoSummonCoroutine;

        private Action<string> summonAction;
        private string lastSelectedOptionId;
        private bool revealCompletedInvoked;
        private bool isBusyReplacing;

        public bool IsShowing => root != null ? root.activeSelf : gameObject.activeSelf;

        public event Action OnRevealCompleted;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (skipRevealButton != null)
                skipRevealButton.onClick.AddListener(SkipReveal);

            BindSummonButtons();
            Hide();
        }

        public void ShowFirstResult(SkillSummonResult result, Action<string> onSummonAction, string currentOptionId)
        {
            summonAction = onSummonAction;
            lastSelectedOptionId = currentOptionId;

            SetPopupVisible(true);
            RefreshSummonButtons();
            ReplaceResult(result);
        }

        public void ReplaceResult(SkillSummonResult result)
        {
            if (result == null) return;
            StartCoroutine(CoReplaceResult(result));
        }

        private IEnumerator CoReplaceResult(SkillSummonResult result)
        {
            isBusyReplacing = true;
            UpdateButtonInteractable();

            StopRevealOnly();
            ResetRevealState();

            yield return StartCoroutine(CoFadeOutOldCards());

            ClearCards();
            BuildCards(result);

            var rootRect = cardRoot as RectTransform;
            if (rootRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

            yield return null;

            bool skipAnim = autoSummonController != null && autoSummonController.IsSkipAnimationOn;

            if (skipAnim)
            {
                ShowAllCardsImmediate();
                InvokeRevealCompletedOnce();
                StartAutoSummonIfNeeded();
            }
            else
            {
                revealCoroutine = StartCoroutine(CoRevealCards());
            }

            isBusyReplacing = false;
            UpdateButtonInteractable();
        }

        private IEnumerator CoFadeOutOldCards()
        {
            float duration = 0.16f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = 1f - Mathf.Clamp01(time / duration);

                for (int i = 0; i < spawnedCards.Count; i++)
                {
                    if (spawnedCards[i] != null && spawnedCards[i].CanvasGroup != null)
                        spawnedCards[i].CanvasGroup.alpha = t;
                }

                yield return null;
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null && spawnedCards[i].CanvasGroup != null)
                    spawnedCards[i].CanvasGroup.alpha = 0f;
            }
        }

        public void Hide()
        {
            StopAllCoroutinesInternal();
            ClearCards();
            SetPopupVisible(false);

            revealCompletedInvoked = false;
            isBusyReplacing = false;
        }

        private void BindSummonButtons()
        {
            if (summonButtonA != null)
                summonButtonA.Init(optionAId, HandleSummonButtonClick);

            if (summonButtonB != null)
                summonButtonB.Init(optionBId, HandleSummonButtonClick);
        }

        private void RefreshSummonButtons()
        {
            if (summonButtonA != null)
                summonButtonA.Refresh();

            if (summonButtonB != null)
                summonButtonB.Refresh();
        }

        private void HandleSummonButtonClick(string optionId)
        {
            if (isBusyReplacing)
                return;

            lastSelectedOptionId = optionId;
            StopAutoSummonOnly();
            summonAction?.Invoke(optionId);
        }

        private void BuildCards(SkillSummonResult result)
        {
            currentGroupedEntries.Clear();
            currentGroupedEntries.AddRange(SkillSummonResultGrouper.Group(result));

            for (int i = 0; i < currentGroupedEntries.Count; i++)
            {
                var entry = currentGroupedEntries[i];
                var visual = rarityVisualConfig != null ? rarityVisualConfig.Get(entry.Grade) : null;

                var card = GetCardFromPool();
                card.Bind(
                    entry,
                    visual != null ? visual.icon : null,
                    visual != null ? visual.topColor : Color.white,
                    visual != null ? visual.bottomColor : Color.white,
                    i == currentGroupedEntries.Count - 1
                );

                spawnedCards.Add(card);
            }
        }

        private SkillSummonSequenceCardUI GetCardFromPool()
        {
            SkillSummonSequenceCardUI card = null;

            int lastIndex = cardPool.Count - 1;
            if (lastIndex >= 0)
            {
                card = cardPool[lastIndex];
                cardPool.RemoveAt(lastIndex);
            }
            else
            {
                card = Instantiate(cardPrefab, cardRoot);
            }

            card.transform.SetParent(cardRoot, false);
            card.transform.SetAsLastSibling();
            card.PrepareForReuse();
            card.SetVisible(true);

            return card;
        }

        private void ReleaseAllSpawnedCardsToPool()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var card = spawnedCards[i];
                if (card == null) continue;

                card.PrepareForReuse();
                card.SetVisible(false);
                cardPool.Add(card);
            }

            spawnedCards.Clear();
            currentGroupedEntries.Clear();
        }

        private IEnumerator CoRevealCards()
        {
            yield return new WaitForSeconds(initialDelay);

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] == null)
                    continue;

                var entry = i < currentGroupedEntries.Count ? currentGroupedEntries[i] : null;
                bool isHighRarity = entry != null && entry.Grade >= SkillSummonGrade.S;
                bool isLastCard = i == spawnedCards.Count - 1;

                if (isHighRarity)
                    yield return new WaitForSeconds(highRarityExtraPause);

                spawnedCards[i].Reveal();

                float delay = Mathf.Lerp(maxRevealInterval, minRevealInterval, spawnedCards.Count <= 1 ? 1f : (float)i / (spawnedCards.Count - 1));

                if (isLastCard)
                    delay += lastCardExtraPause;

                yield return new WaitForSeconds(delay);
            }

            revealCoroutine = null;
            InvokeRevealCompletedOnce();
            StartAutoSummonIfNeeded();
        }

        private void SkipReveal()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }

            ShowAllCardsImmediate();
            InvokeRevealCompletedOnce();
            StartAutoSummonIfNeeded();
        }

        private void ShowAllCardsImmediate()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    spawnedCards[i].ShowImmediate();
            }
        }

        private void StartAutoSummonIfNeeded()
        {
            if (autoSummonController == null || !autoSummonController.IsAutoSummonOn)
                return;

            StopAutoSummonOnly();
            autoSummonCoroutine = StartCoroutine(CoAutoSummon());
        }

        private IEnumerator CoAutoSummon()
        {
            yield return new WaitForSeconds(autoSummonDelay);

            if (autoSummonController == null || !autoSummonController.IsAutoSummonOn)
            {
                autoSummonCoroutine = null;
                yield break;
            }

            string optionId = string.IsNullOrEmpty(lastSelectedOptionId) ? optionAId : lastSelectedOptionId;
            summonAction?.Invoke(optionId);
            autoSummonCoroutine = null;
        }

        private void InvokeRevealCompletedOnce()
        {
            if (revealCompletedInvoked) return;

            revealCompletedInvoked = true;
            OnRevealCompleted?.Invoke();
        }

        private void ResetRevealState()
        {
            revealCompletedInvoked = false;
        }

        public void SetBusyReplacing(bool value)
        {
            isBusyReplacing = value;
            UpdateButtonInteractable();
        }

        private void UpdateButtonInteractable()
        {
            bool canClick = !isBusyReplacing;

            if (summonButtonA != null)
                summonButtonA.SetInteractable(canClick);

            if (summonButtonB != null)
                summonButtonB.SetInteractable(canClick);
        }

        private void StopRevealOnly()
        {
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }
        }

        private void StopAutoSummonOnly()
        {
            if (autoSummonCoroutine != null)
            {
                StopCoroutine(autoSummonCoroutine);
                autoSummonCoroutine = null;
            }
        }

        private void StopAllCoroutinesInternal()
        {
            StopRevealOnly();
            StopAutoSummonOnly();
        }

        private void SetPopupVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private void ClearCards()
        {
            ReleaseAllSpawnedCardsToPool();
        }
    }
}