using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.DungeonSystem.Views.UI;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.DungeonSystem.Views
{
    public class DungeonView : AnimatedUIView
    {
        [Header("View references")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtStage;
        [SerializeField] private TextMeshProUGUI txtHighestStage;
        [SerializeField] private TextMeshProUGUI txtTicket;

        [SerializeField] private Button btnStart;
        [SerializeField] private Button btnSweep;
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnPrev;

        [Header("Reward references")] [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField] private UIDungeonReward rewardPrefab;

        // --- Private Fields ---
        private List<UIDungeonReward> _rewards = new();

        /// <summary>
        /// thong tin khi stage changed, type dungeon và hướng đi.
        /// 1: type dungeon
        /// 2: huong toi truoc hay sau
        /// </summary>
        private Func<EDungeonType, int, UniTask<List<ItemRewardSet>>> _onStageChanged;

        private EDungeonType _type;
        private Action _onStart;

        private int _currentStageIdx;
        private int _maxStageIdx;

        private void Awake()
        {
            btnNext.onClick.AddListener(OnClickNext);
            btnPrev.onClick.AddListener(OnClickPrev);
            btnStart.onClick.AddListener(OnClickStart);
        }

        private void OnClickStart()
        {
            _onStart?.Invoke();
        }

        private void OnClickNext()
        {
            OnStageChange(1).Forget();
        }

        private async UniTask OnStageChange(int direction)
        {
            _currentStageIdx = (Mathf.Max(0, _currentStageIdx + direction)) % _maxStageIdx;
            txtStage.SetText($"{_currentStageIdx + 1}");
            RefreshBtnDirection();

            if (_onStageChanged != null)
            {
                var rewards = await _onStageChanged.Invoke(_type, _currentStageIdx + 1);
                RefreshRewards(rewards);
            }
        }

        private void OnClickPrev()
        {
            OnStageChange(-1).Forget();
        }

        private void RefreshBtnDirection()
        {
            if (_currentStageIdx >= _maxStageIdx - 1)
            {
                btnNext.gameObject.SetActive(false);
                btnPrev.gameObject.SetActive(true);
            }
            else if (_currentStageIdx <= 0)
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(false);
            }
            else
            {
                btnNext.gameObject.SetActive(true);
                btnPrev.gameObject.SetActive(true);
            }
        }

        public void Bind(EDungeonType type, int ticket, string title, int currentStageIdx, int maxStageIdx, Action onStart,
            Func<EDungeonType, int, UniTask<List<ItemRewardSet>>> onStageChanged)
        {
            _type = type;
            _onStart = onStart;
            _currentStageIdx = currentStageIdx;
            _maxStageIdx = maxStageIdx;
            _onStageChanged = onStageChanged;

            txtTitle.SetText(title);
            txtTicket.SetText($"{ticket}");
            txtHighestStage.SetText($"Cửa ải {_currentStageIdx + 1}");

            OnStageChange(0).Forget();
            RefreshBtnDirection();
        }

        private void RefreshRewards(List<ItemRewardSet> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                var entry = rewards[index];

                if (_rewards.Count > index)
                {
                    var clone = _rewards[index];
                    clone.gameObject.SetActive(true);
                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tier);
                    clone.BindQuantity(entry.Quantity);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tier);
                    clone.BindQuantity(entry.Quantity);
                    _rewards.Add(clone);
                }
            }

            // hide cac object ko su dung
            for (int i = rewards.Count; i < _rewards.Count; i++)
            {
                _rewards[i].gameObject.SetActive(false);
            }
        }
    }
}