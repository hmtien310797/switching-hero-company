using System;
using System.Collections.Generic;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.DungeonSystem.Views.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.DungeonSystem.Views
{
    public class DungeonMainView : AnimatedUIView
    {
        [Header("View references")]
        [SerializeField]
        private Image bgImg;

        [SerializeField]
        private Button btnChallenge;

        [Header("Button references")]
        [SerializeField]
        private List<UIDungeonBtn> btnDungeons;

        [Header("Reward references")]
        [SerializeField]
        private UIItemSlot rewardPrefab;

        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        [Range(0f, 1f)]
        private float rewardScale;

        // --- Private Fields ---
        private List<UIItemSlot> _slots = new();
        private UIDungeonBtn _selectedDungeon;

        private void Awake()
        {
            btnChallenge.onClick.AddListener(() => OnClickChallenge().Forget());
        }

        private async UniTask OnClickChallenge()
        {
            var dungeonId = _selectedDungeon.DungeonId;
            await DatabaseManager.Instance.EnsureDungeonStageTableAsync(dungeonId); // no-op if already cached

            var dungeonKey = DatabaseManager.Instance.GetDungeonKey(dungeonId);
            var state = await NakamaClient.Instance.GetDungeonStateAsync();
            DungeonInfo info = null;
            state.Dungeons?.TryGetValue(dungeonKey ?? string.Empty, out info);

            // maxStage/ticketRequired are per-dungeon server truth — GetDungeonMaxStage/
            // GetDungeonTicketRequest (local DungeonDatabaseSO data) are only a fallback for
            // dungeons dungeon/state doesn't recognize yet (e.g. boss_dragon, not wired
            // server-side), since local StageCount is a stale placeholder (500) otherwise.
            // ticketOwned is a shared balance across all dungeons, not per-dungeon — it comes from
            // state.TicketBalance regardless of whether this specific dungeonKey has an info entry.
            var ticketRequired = info != null ? info.TicketRequest : DatabaseManager.Instance.GetDungeonTicketRequest(dungeonId);

            //set 500 for test, expect info.highestStageCleard is alway available
            var startIdx = Mathf.Clamp(info?.HighestStageCleared ?? 0, 0, Mathf.Max(0, 500 - 1));
            var maxStage = startIdx + 1;
            var ticketOwned = (int)state.TicketBalance;

            var ui = await UIManager.Instance.OpenPopupAsync<DungeonView>();
            var title = DatabaseManager.Instance.GetDungeonTitle(dungeonId);
            var visual = DatabaseManager.Instance.DungeonVisualDb.Get(dungeonId);

            ui.Bind(
                visual?.banner, dungeonId,
                ticketOwned, ticketRequired,
                title, startIdx, maxStage,
                OnClickStart,
                OnStageChangedAsync
            );
        }

        private IReadOnlyList<ItemRewardData> OnStageChangedAsync(int dungeonId, int stageIdx)
        {
            return DatabaseManager.Instance.GetDungeonRewards(dungeonId, stageIdx);
        }

        private void OnClickStart(int dungeonId, int stageIdx)
        {
            GameEventManager.Trigger(GameEvents.OnSelectedDungeonStage, dungeonId, stageIdx);
        }

        private void OnEnable()
        {
            InitDungeon();
            AutoSelectFirstDungeon();
        }

        private void AutoSelectFirstDungeon()
        {
            OnClickDungeon(0);
        }

        private void InitDungeon()
        {
            for (var index = 0; index < btnDungeons.Count; index++)
            {
                var entry = btnDungeons[index];
                entry.Bind(index, string.Empty, OnClickDungeon);
                entry.SetStatus(ETabPresetStatus.Normal);
            }
        }

        private void OnClickDungeon(int idx)
        {
            if (_selectedDungeon != null)
            {
                _selectedDungeon.SetStatus(ETabPresetStatus.Normal);
                _selectedDungeon = null;
            }

            _selectedDungeon = btnDungeons[idx];
            _selectedDungeon.SetStatus(ETabPresetStatus.Selected);

            // lay ra thong tin sprite
            var spriteInfo = DatabaseManager.Instance.DungeonVisualDb.Get(_selectedDungeon.DungeonId);

            if (spriteInfo != null)
            {
                RefreshBg(spriteInfo.background);
            }

            RefreshRewardsAsync(_selectedDungeon.DungeonId).Forget();
        }

        private async UniTaskVoid RefreshRewardsAsync(int dungeonId)
        {
            await DatabaseManager.Instance.EnsureDungeonStageTableAsync(dungeonId);
            RefreshRewards(dungeonId);
        }

        private void RefreshBg(Sprite newBg)
        {
            bgImg.sprite = newBg;
            bgImg.SetNativeSize();
        }

        private void RefreshRewards(int dungeonId)
        {
            var rewards = DatabaseManager.Instance.GetDungeonRewards(dungeonId);

            for (var index = 0; index < rewards.Count; index++)
            {
                var entry = rewards[index];

                if (_slots.Count > index)
                {
                    var clone = _slots[index];
                    clone.transform.localScale = Vector3.one * rewardScale;

                    clone.gameObject.SetActive(true);

                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background,
                        entry.TierInfo.tierIcon);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.transform.localScale = Vector3.one * rewardScale;

                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background,
                        entry.TierInfo.tierIcon);

                    _slots.Add(clone);
                }
            }

            // hide cac object ko su dung
            for (int i = rewards.Count; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(false);
            }
        }
    }
}