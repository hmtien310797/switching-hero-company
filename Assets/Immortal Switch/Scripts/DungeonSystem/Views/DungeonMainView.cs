using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.DungeonSystem.Views.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.DungeonSystem.Views
{
    public class DungeonMainView : AnimatedUIView
    {
        [Header("View references")] [SerializeField]
        private Image bgImg;

        [SerializeField] private Button btnChallenge;

        [Header("Button references")] [SerializeField]
        private List<UIDungeonBtn> btnDungeons;

        [Header("Reward references")] [SerializeField]
        private UIBagSlot rewardPrefab;

        [SerializeField] private RectTransform rewardContainer;
        [SerializeField] [Range(0f, 1f)] private float rewardScale;

        // --- Private Fields ---
        private List<UIBagSlot> _slots = new();
        private UIDungeonBtn _selectedDungeon;

        private void Awake()
        {
            btnChallenge.onClick.AddListener(() => OnClickChallenge().Forget());
        }

        private async UniTask OnClickChallenge()
        {
            if (_selectedDungeon != null)
            {
                var dungeonKey = GetDungeonKey(_selectedDungeon.Type);
                var maxStage = DatabaseManager.Instance.GetDungeonRewardsMaxStage(dungeonKey);
                var ui = await UIManager.Instance.OpenPopupAsync<DungeonView>();
                var title = DatabaseManager.Instance.GetDungeonTitle(dungeonKey);
                var ticket = DatabaseManager.Instance.GetDungeonTicketRequest(dungeonKey);
                ui.Bind(_selectedDungeon.Type, ticket, title, 0, maxStage, OnClickStart, OnStageChangedAsync);
            }
        }

        private async UniTask<List<ItemRewardSet>> OnStageChangedAsync(EDungeonType type, int stageIdx)
        {
            var dungeonKey = GetDungeonKey(type);
            var rewards = await DatabaseManager.Instance.GetDungeonRewards(dungeonKey, stageIdx);
            return rewards;
        }

        private void OnClickStart()
        {
            if (_selectedDungeon != null)
            {
                DungeonSystemManager.Instance.NotifySelectedChallenge(_selectedDungeon.Type);
            }
        }

        private void OnEnable()
        {
            InitDungeon();
            AutoSelectFirstDungeon();
        }

        private void AutoSelectFirstDungeon()
        {
            for (int i = 0; i < btnDungeons.Count; i++)
            {
                var entry = btnDungeons[i];

                if (entry.Type == EDungeonType.Treasure)
                {
                    OnClickDungeon(i);
                    break;
                }
            }
        }

        private void InitDungeon()
        {
            for (var index = 0; index < btnDungeons.Count; index++)
            {
                var entry = btnDungeons[index];
                entry.Bind(index, string.Empty, OnClickDungeon);
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
            var spriteInfo = DatabaseManager.Instance.DungeonDb.Get(_selectedDungeon.Type);

            if (spriteInfo != null)
            {
                RefreshBg(spriteInfo.background);
            }

            RefreshRewards(_selectedDungeon.Type).Forget();
        }

        private void RefreshBg(Sprite newBg)
        {
            bgImg.sprite = newBg;
            bgImg.SetNativeSize();
        }

        private async UniTask RefreshRewards(EDungeonType dungeon)
        {
            var dungeonKey = GetDungeonKey(dungeon);
            var rewards = await DatabaseManager.Instance.GetDungeonRewards(dungeonKey);

            for (var index = 0; index < rewards.Count; index++)
            {
                var entry = rewards[index];

                if (_slots.Count > index)
                {
                    var clone = _slots[index];
                    clone.transform.localScale = Vector3.one * rewardScale;

                    clone.gameObject.SetActive(true);
                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tier);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.transform.localScale = Vector3.one * rewardScale;

                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tier);
                    _slots.Add(clone);
                }
            }

            // hide cac object ko su dung
            for (int i = rewards.Count; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(false);
            }
        }

        private string GetDungeonKey(EDungeonType dungeon)
        {
            return dungeon switch
            {
                EDungeonType.Treasure => "treasure",
                EDungeonType.Artifact => "relic",
                EDungeonType.Diamond => "awakening",
                EDungeonType.Equipment => "weapon",
                _ => throw new ArgumentOutOfRangeException(nameof(dungeon), dungeon, null),
            };
        }
    }
}