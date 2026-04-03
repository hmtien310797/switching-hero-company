using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonSequencePopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private SharedSummonSequenceCardUI cardPrefab;

        [Header("Top / Utility")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button skipRevealButton;

        [Header("Bottom Summon Buttons")]
        [SerializeField] private BaseSummonButtonUI summonButtonA;
        [SerializeField] private BaseSummonButtonUI summonButtonB;
        [SerializeField] private string optionAId = "summon_10";
        [SerializeField] private string optionBId = "summon_50";

        [Header("Auto / Skip")]
        [SerializeField] private Toggle autoSummonToggle;
        [SerializeField] private Toggle skipAnimationToggle;
        [SerializeField] private float autoSummonDelay = 0.35f;

        private readonly List<SharedSummonSequenceCardUI> spawnedCards = new();
        private readonly List<SharedSummonSequenceItemData> currentItems = new();

        private Coroutine autoSummonCoroutine;
        private Action<string> summonAction;
        private string lastSelectedOptionId;
        private bool isBusyReplacing;
        private bool isBound;

        public bool IsShowing => root != null ? root.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
            skipRevealButton?.onClick.AddListener(ShowAllCardsImmediate);
            BindSummonButtonsIfNeeded();
            Hide();
        }

        private void BindSummonButtonsIfNeeded()
        {
            if (isBound)
                return;

            summonButtonA?.Init(optionAId, HandleSummonButtonClick);
            summonButtonB?.Init(optionBId, HandleSummonButtonClick);

            isBound = true;
        }

        public void ShowFirstResult(List<SharedSummonSequenceItemData> items, Action<string> onSummonAction, string currentOptionId)
        {
            summonAction = onSummonAction;
            lastSelectedOptionId = currentOptionId;

            SetPopupVisible(true);
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
            ReplaceResult(items);
        }

        public void ReplaceResult(List<SharedSummonSequenceItemData> items)
        {
            if (items == null)
                return;

            ClearCards();

            currentItems.Clear();
            currentItems.AddRange(items);

            for (int i = 0; i < currentItems.Count; i++)
            {
                var card = Instantiate(cardPrefab, cardRoot);
                card.PrepareForReuse();
                card.Bind(currentItems[i]);
                card.SetVisible(true);
                spawnedCards.Add(card);
            }

            bool skipAnim = skipAnimationToggle != null && skipAnimationToggle.isOn;
            if (skipAnim)
                ShowAllCardsImmediate();
            else
            {
                for (int i = 0; i < spawnedCards.Count; i++)
                    spawnedCards[i].Reveal();
            }

            StartAutoSummonIfNeeded();
        }

        public void Hide()
        {
            StopAutoSummonOnly();
            ClearCards();
            SetPopupVisible(false);
            isBusyReplacing = false;
        }

        public void SetBusyReplacing(bool value)
        {
            isBusyReplacing = value;
            summonButtonA?.SetInteractable(!value);
            summonButtonB?.SetInteractable(!value);
        }

        private void HandleSummonButtonClick(string optionId)
        {
            if (isBusyReplacing)
                return;

            lastSelectedOptionId = optionId;
            StopAutoSummonOnly();
            summonAction?.Invoke(optionId);
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
            if (autoSummonToggle == null || !autoSummonToggle.isOn)
                return;

            StopAutoSummonOnly();
            autoSummonCoroutine = StartCoroutine(CoAutoSummon());
        }

        private IEnumerator CoAutoSummon()
        {
            yield return new WaitForSeconds(autoSummonDelay);

            if (autoSummonToggle == null || !autoSummonToggle.isOn)
            {
                autoSummonCoroutine = null;
                yield break;
            }

            string optionId = string.IsNullOrEmpty(lastSelectedOptionId) ? optionAId : lastSelectedOptionId;
            summonAction?.Invoke(optionId);
            autoSummonCoroutine = null;
        }

        private void StopAutoSummonOnly()
        {
            if (autoSummonCoroutine != null)
            {
                StopCoroutine(autoSummonCoroutine);
                autoSummonCoroutine = null;
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
            currentItems.Clear();
        }

        private void SetPopupVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}