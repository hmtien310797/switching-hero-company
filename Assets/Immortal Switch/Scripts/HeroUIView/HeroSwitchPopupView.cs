using System.Collections.Generic;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroSwitchPopupView : AnimatedUIView
    {
        [Header("Data")]
        [SerializeField] private HeroProgressionDatabaseSO heroDatabase;
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
        
        [Header("Source Select Hint")]
        [SerializeField] private RectTransform slot1Arrow;
        [SerializeField] private RectTransform slot2Arrow;
        [SerializeField] private float arrowMoveDistance = 18f;
        [SerializeField] private float arrowMoveDuration = 0.55f;
        
        [Header("Root")] [SerializeField] private Button btnClose;

        private readonly List<HeroCollectionItemUI> spawnedItems = new();
        private PvEBattleController battleController;

        private int selectedSourceHeroId = -1;
        private int selectedTargetHeroId = -1;
        
        private Tween slot1ArrowTween;
        private Tween slot2ArrowTween;

        private Vector2 slot1ArrowAnchoredPos;
        private Vector2 slot2ArrowAnchoredPos;
        private int heroSwitchSlotUIIndex;
        
        public override void OnShow(object args)
        {
            RefreshView();
            base.OnShow(args);
        }

        public override void OnHide()
        {
            StopSourceArrowHint();
            base.OnHide();
        }

        private void OnDestroy()
        {
            StopSourceArrowHint();
        }

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
            battleController = PvEBattleController.Instance;
            if (slot1Arrow != null)
                slot1ArrowAnchoredPos = slot1Arrow.anchoredPosition;

            if (slot2Arrow != null)
                slot2ArrowAnchoredPos = slot2Arrow.anchoredPosition;
        }
        
        private void OnClickClose()
        {
            UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>().Forget();
        }

        public void RefreshView()
        {
            if (battleController == null || heroDatabase == null)
                return;

            if (HeroProgressionManager.Instance == null || HeroProgressionManager.Instance.Service == null)
                return;

            var activeIds = UserDataCache.Instance.InBattleHeroIdList;
            if (activeIds == null || activeIds.Count < 2)
                return;

            selectedSourceHeroId = -1;
            selectedTargetHeroId = -1;

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
                hero1,
                heroDatabase, service, heroUIIconConfig);

            var data2 = HeroCollectionItemViewDataFactory.Build(
                hero2,
                heroDatabase, service, heroUIIconConfig);

            slot1UI?.Bind(1, data1, OnClickSourceSlot);
            slot2UI?.Bind(2, data2, OnClickSourceSlot);
        }

        private void RebuildCandidateList(List<int> activeIds)
        {
            ClearItems();

            var allHeroes = MasterDataCache.Instance.GetAllHeroData();
            var service = HeroProgressionManager.Instance.Service;
            var allData = new List<HeroCollectionItemViewData>();

            for (int i = 0; i < allHeroes.Count; i++)
            {
                var hero = allHeroes[i];
                if (hero == null) continue;

                var data = HeroCollectionItemViewDataFactory.Build(
                    hero,
                    heroDatabase,
                    service,
                    heroUIIconConfig);

                if (data == null) continue;
                if (!data.IsAcquired) continue;

                // QUAN TRỌNG: hero đang ở sân thì không được xuất hiện trong list
                if (activeIds.Contains(data.HeroId)) continue;

                allData.Add(data);
            }

            allData.Sort(HeroCollectionItemViewDataFactory.Sort);

            foreach (var data in allData)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(data);
                item.SetClickCallback(OnClickCandidateItem);
                item.SetButtonInteractable(true);
                item.SetDimmed(false);
                item.SetSelected(false);
                item.SetReadyHighlight(false);

                spawnedItems.Add(item);
            }
        }

        private void OnClickSourceSlot(int heroId, int heroSwitchSlotIndex)
        {
            if (heroId <= 0) return;

            selectedSourceHeroId = heroId;
            heroSwitchSlotUIIndex = heroSwitchSlotIndex;
            RefreshSelectionVisualState();
        }

        private void OnClickCandidateItem(HeroCollectionItemUI item)
        {
            if (item == null || item.Data == null)
                return;

            selectedTargetHeroId = item.Data.HeroId;
            RefreshSelectionVisualState();
        }

        private bool HasValidSelection()
        {
            if (battleController == null) return false;
            if (selectedSourceHeroId <= 0) return false;
            if (selectedTargetHeroId <= 0) return false;
            if (selectedSourceHeroId == selectedTargetHeroId) return false;
            if (!battleController.IsHeroCurrentlyActive(selectedSourceHeroId)) return false;
            if (battleController.IsHeroCurrentlyActive(selectedTargetHeroId)) return false;
            HeroDataSO targetSourceHeroIdData = MasterDataCache.Instance.GetHeroDataById(selectedTargetHeroId);
            if (heroSwitchSlotUIIndex == 1)
            {
                if (slot2UI.heroSlotClass != targetSourceHeroIdData.HeroClass) return true;
                UIManager.Instance.ShowToast("Can Not Assign Hero Of The Same Class");
                return false;
            }
            if (slot1UI.heroSlotClass != targetSourceHeroIdData.HeroClass) return true;
            UIManager.Instance.ShowToast("Can Not Assign Hero Of The Same Class");
            return false;
        }

        private void RefreshSelectionVisualState()
        {
            bool ready = HasValidSelection();

            if (instructionText != null)
            {
                if (selectedSourceHeroId <= 0 && selectedTargetHeroId <= 0)
                    instructionText.text = "Vui lòng chọn Anh hùng";
                else if (selectedSourceHeroId <= 0)
                    instructionText.text = "Vui lòng chọn Anh hùng để thay ra";
                else if (selectedTargetHeroId <= 0)
                    instructionText.text = "Vui lòng chọn Anh hùng để thay vào";
                else
                    instructionText.text = "Sẵn sàng thay đổi";
            }
            
            if (selectedSourceHeroId <= 0)
                PlaySourceArrowHint();
            else
                StopSourceArrowHint();

            if (slot1UI != null)
            {
                bool selected = slot1UI.HeroId == selectedSourceHeroId;
                slot1UI.SetSelected(selected);
                slot1UI.SetReadyHighlight(ready && selected);
            }

            if (slot2UI != null)
            {
                bool selected = slot2UI.HeroId == selectedSourceHeroId;
                slot2UI.SetSelected(selected);
                slot2UI.SetReadyHighlight(ready && selected);
            }

            for (int i = 0; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item == null || item.Data == null) continue;

                bool selected = item.Data.HeroId == selectedTargetHeroId;
                item.SetSelected(selected);
                item.SetReadyHighlight(ready && selected);
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
        
        private void PlaySourceArrowHint()
        {
            StopSourceArrowHint();

            if (slot1Arrow != null)
            {
                slot1Arrow.gameObject.SetActive(true);
                slot1Arrow.anchoredPosition = slot1ArrowAnchoredPos;

                slot1ArrowTween = slot1Arrow.DOAnchorPosY(
                        slot1ArrowAnchoredPos.y - arrowMoveDistance,
                        arrowMoveDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (slot2Arrow != null)
            {
                slot2Arrow.gameObject.SetActive(true);
                slot2Arrow.anchoredPosition = slot2ArrowAnchoredPos;

                slot2ArrowTween = slot2Arrow.DOAnchorPosY(
                        slot2ArrowAnchoredPos.y - arrowMoveDistance,
                        arrowMoveDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void StopSourceArrowHint()
        {
            slot1ArrowTween?.Kill();
            slot2ArrowTween?.Kill();

            slot1ArrowTween = null;
            slot2ArrowTween = null;

            if (slot1Arrow != null)
            {
                slot1Arrow.anchoredPosition = slot1ArrowAnchoredPos;
                slot1Arrow.gameObject.SetActive(false);
            }

            if (slot2Arrow != null)
            {
                slot2Arrow.anchoredPosition = slot2ArrowAnchoredPos;
                slot2Arrow.gameObject.SetActive(false);
            }
        }

        private void ConfirmSwitch()
        {
            if (!HasValidSelection())
                return;

            battleController.RequestSwitchHero(selectedSourceHeroId, selectedTargetHeroId);
            UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>().Forget();
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