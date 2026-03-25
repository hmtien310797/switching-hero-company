using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.UI;
using Scripts.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroSwitchPopupView : AnimatedUIView
    {
        [Header("Data")]
        [SerializeField] private HeroProgressionDatabaseSO heroDatabase;
        [SerializeField] private HeroRarityVisualConfigSO heroRarityVisualConfig;
        [SerializeField] private HeroUIIconConfigSO heroUIIconConfig;

        [Header("Top Slots")]
        [SerializeField] private HeroSwitchSlotUI slot1UI;
        [SerializeField] private HeroSwitchSlotUI slot2UI;

        [Header("Candidate List")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private HeroCollectionItemUI itemPrefab;

        [Header("Texts")]
        [SerializeField] private TMP_Text instructionText;

        [Header("Confirm")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private CanvasGroup confirmCanvasGroup;
        [SerializeField] private GameObject confirmReadyObject;
        [SerializeField] private GameObject confirmDisabledObject;

        private readonly List<HeroCollectionItemUI> spawnedItems = new();

        private int selectedSourceHeroId = -1;
        private int selectedTargetHeroId = -1;
        private HeroCollectionItemUI selectedTargetItem;
        private PvEBattleController battleController;

        private void Awake()
        {
            battleController = PvEBattleController.Instance;
        }

        private void OnEnable()
        {
            RefreshView();
        }

        public void RefreshView()
        {
            if (battleController == null)
            {
                Debug.LogWarning("HeroSwitchPopupView missing battleController.");
                return;
            }

            if (heroDatabase == null)
            {
                Debug.LogWarning("HeroSwitchPopupView missing heroDatabase.");
                return;
            }

            if (HeroProgressionManager.Instance == null || HeroProgressionManager.Instance.Service == null)
            {
                Debug.LogWarning("HeroSwitchPopupView missing HeroProgressionManager.");
                return;
            }

            var activeIds = battleController.GetCurrentSwitchHeroIds();
            if (activeIds == null || activeIds.Count < 2)
            {
                Debug.LogWarning("HeroSwitchPopupView active hero ids invalid.");
                return;
            }

            selectedSourceHeroId = -1;
            selectedTargetHeroId = -1;
            selectedTargetItem = null;

            BindTopSlots(activeIds);
            RebuildCandidateList(activeIds);
            RefreshSelectionVisualState();
        }

        private void BindTopSlots(List<int> activeIds)
        {
            var service = HeroProgressionManager.Instance.Service;

            var hero1 = heroDatabase.GetHero(activeIds[0]);
            var hero2 = heroDatabase.GetHero(activeIds[1]);

            var data1 = HeroCollectionItemViewDataFactory.Build(
                hero1, heroDatabase, service, heroRarityVisualConfig, heroUIIconConfig);

            var data2 = HeroCollectionItemViewDataFactory.Build(
                hero2, heroDatabase, service, heroRarityVisualConfig, heroUIIconConfig);

            if (slot1UI != null)
                slot1UI.Bind(1, data1, OnClickSourceSlot);

            if (slot2UI != null)
                slot2UI.Bind(2, data2, OnClickSourceSlot);
        }

        private void RebuildCandidateList(List<int> activeIds)
        {
            ClearItems();

            var service = HeroProgressionManager.Instance.Service;
            var allData = new List<HeroCollectionItemViewData>();

            for (int i = 0; i < heroDatabase.Heroes.Count; i++)
            {
                var hero = heroDatabase.Heroes[i];
                if (hero == null) continue;

                var data = HeroCollectionItemViewDataFactory.Build(
                    hero,
                    heroDatabase,
                    service,
                    heroRarityVisualConfig,
                    heroUIIconConfig);

                if (data == null) continue;
                if (!data.IsAcquired) continue;

                allData.Add(data);
            }

            allData.Sort(HeroCollectionItemViewDataFactory.Sort);

            for (int i = 0; i < allData.Count; i++)
            {
                var data = allData[i];
                bool canSelect = !activeIds.Contains(data.HeroId);

                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(data);
                item.SetClickCallback(OnClickCandidateItem);
                item.SetSelected(false);
                item.SetReadyHighlight(false);

                spawnedItems.Add(item);
            }
        }

        private void OnClickSourceSlot(int heroId)
        {
            if (heroId <= 0)
                return;

            selectedSourceHeroId = heroId;
            RefreshSelectionVisualState();
        }

        private void OnClickCandidateItem(HeroCollectionItemUI item)
        {
            if (item == null || item.Data == null)
                return;

            if (battleController == null)
                return;

            if (!battleController.CanSwitchHero(selectedSourceHeroId, item.Data.HeroId))
            {
                // Trường hợp chưa chọn source hoặc target không hợp lệ
                if (selectedSourceHeroId <= 0)
                    return;

                // Nếu target không hợp lệ vì đang active thì cũng bỏ qua
                if (battleController.IsHeroCurrentlyActive(item.Data.HeroId))
                    return;
            }

            selectedTargetItem = item;
            selectedTargetHeroId = item.Data.HeroId;

            RefreshSelectionVisualState();
        }

        private bool HasValidSelection()
        {
            return battleController != null &&
                   battleController.CanSwitchHero(selectedSourceHeroId, selectedTargetHeroId);
        }

        private void RefreshSelectionVisualState()
        {
            bool ready = HasValidSelection();

            if (instructionText != null)
            {
                if (selectedSourceHeroId <= 0)
                    instructionText.text = "Please select the hero to switch";
                else if (selectedTargetHeroId <= 0)
                    instructionText.text = "Please select the hero to replace";
                else
                    instructionText.text = "Ready to switch";
            }

            if (slot1UI != null)
            {
                bool isSelectedSource = slot1UI.HeroId == selectedSourceHeroId;
                slot1UI.SetSelected(isSelectedSource);
                slot1UI.SetReadyHighlight(ready && isSelectedSource);
            }

            if (slot2UI != null)
            {
                bool isSelectedSource = slot2UI.HeroId == selectedSourceHeroId;
                slot2UI.SetSelected(isSelectedSource);
                slot2UI.SetReadyHighlight(ready && isSelectedSource);
            }

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item == null || item.Data == null) continue;

                bool isSelectedTarget = item.Data.HeroId == selectedTargetHeroId;
                item.SetSelected(isSelectedTarget);
                item.SetReadyHighlight(ready && isSelectedTarget);
            }

            RefreshConfirmVisual(ready);
        }

        private void RefreshConfirmVisual(bool ready)
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmSwitch);
                confirmButton.interactable = ready;
            }

            if (confirmCanvasGroup != null)
                confirmCanvasGroup.alpha = ready ? 1f : 0.6f;

            if (confirmReadyObject != null)
                confirmReadyObject.SetActive(ready);

            if (confirmDisabledObject != null)
                confirmDisabledObject.SetActive(!ready);
        }

        private void ConfirmSwitch()
        {
            if (!HasValidSelection())
                return;

            battleController.RequestSwitchHero(selectedSourceHeroId, selectedTargetHeroId);
            gameObject.SetActive(false);
        }

        private void ClearItems()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                    Destroy(spawnedItems[i].gameObject);
            }

            spawnedItems.Clear();
        }
    }
}