using System;
using System.Collections;
using System.Collections.Generic;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonSequencePopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private WeaponSummonSequenceCardUI cardPrefab;

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

        private readonly List<WeaponSummonSequenceCardUI> spawnedCards = new();
        private readonly List<WeaponSummonSequenceCardUI> cardPool = new();
        private readonly List<WeaponSummonGroupedResultEntry> currentGroupedEntries = new();

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

        public void ShowFirstResult(WeaponSummonResult result, Action<string> onSummonAction, string currentOptionId)
        {
            summonAction = onSummonAction;
            lastSelectedOptionId = currentOptionId;

            SetPopupVisible(true);
            RefreshSummonButtons();
            ReplaceResult(result);
        }

        public void ReplaceResult(WeaponSummonResult result)
        {
            if (result == null)
                return;

            StartCoroutine(CoReplaceResult(result));
        }

        public void Hide()
        {
            StopAllCoroutinesInternal();
            ClearCards();
            SetPopupVisible(false);

            revealCompletedInvoked = false;
            isBusyReplacing = false;
        }

        public void SetBusyReplacing(bool value)
        {
            isBusyReplacing = value;
            UpdateButtonInteractable();
        }

        private IEnumerator CoReplaceResult(WeaponSummonResult result)
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

        private void BindSummonButtons()
        {
            if (summonButtonA != null)
                summonButtonA.Init(optionAId, HandleSummonButtonClick, SummonCategory.Weapon);

            if (summonButtonB != null)
                summonButtonB.Init(optionBId, HandleSummonButtonClick, SummonCategory.Weapon);
        }

        private void RefreshSummonButtons()
        {
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
        }

        private void HandleSummonButtonClick(string optionId)
        {
            if (isBusyReplacing)
                return;

            lastSelectedOptionId = optionId;
            StopAutoSummonOnly();
            summonAction?.Invoke(optionId);
        }

        private void BuildCards(WeaponSummonResult result)
        {
            currentGroupedEntries.Clear();
            currentGroupedEntries.AddRange(WeaponSummonResultGrouper.Group(result));

            for (int i = 0; i < currentGroupedEntries.Count; i++)
            {
                var entry = currentGroupedEntries[i];
                var card = GetCardFromPool();

                bool isLastCard = i == currentGroupedEntries.Count - 1;
                card.Bind(entry, isLastCard);
                spawnedCards.Add(card);
            }
        }

        private WeaponSummonSequenceCardUI GetCardFromPool()
        {
            WeaponSummonSequenceCardUI card = null;

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
                if (card == null)
                    continue;

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
                bool isHighRarity = entry != null && IsHighValue(entry);
                bool isLastCard = i == spawnedCards.Count - 1;

                if (isHighRarity)
                    yield return new WaitForSeconds(highRarityExtraPause);

                spawnedCards[i].Reveal();

                float delay = Mathf.Lerp(
                    maxRevealInterval,
                    minRevealInterval,
                    spawnedCards.Count <= 1 ? 1f : (float)i / (spawnedCards.Count - 1));

                if (isLastCard)
                    delay += lastCardExtraPause;

                yield return new WaitForSeconds(delay);
            }

            revealCoroutine = null;
            InvokeRevealCompletedOnce();
            StartAutoSummonIfNeeded();
        }

        private bool IsHighValue(WeaponSummonGroupedResultEntry entry)
        {
            if (entry == null)
                return false;

            return entry.Tier.ToString() == "S" ||
                   entry.Tier.ToString() == "SS" ||
                   entry.Star >= 4;
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
            if (revealCompletedInvoked)
                return;

            revealCompletedInvoked = true;
            OnRevealCompleted?.Invoke();
        }

        private void ResetRevealState()
        {
            revealCompletedInvoked = false;
        }

        private void UpdateButtonInteractable()
        {
            bool canClick = !isBusyReplacing;

            summonButtonA?.SetInteractable(canClick);
            summonButtonB?.SetInteractable(canClick);
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