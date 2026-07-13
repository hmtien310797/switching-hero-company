using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.DungeonSystem.Models;
using Immortal_Switch.Scripts.DungeonSystem.Views.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.DungeonSystem.Views
{
    public class DungeonView : AnimatedUIView
    {
        [Header("View references")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtDescription;

        [SerializeField]
        private TextMeshProUGUI txtStage;

        [SerializeField]
        private TextMeshProUGUI txtHighestStage;

        [SerializeField]
        private TextMeshProUGUI txtTicket;

        [SerializeField]
        private Button btnStart;

        [SerializeField]
        private Button btnSweep;

        [SerializeField]
        private Button btnNext;

        [SerializeField]
        private Button btnPrev;

        [SerializeField]
        private Toggle consecutiveChallengeToggle;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIReward rewardPrefab;

        [SerializeField] private TextMeshProUGUI txtTicketDungeonPlayer;
        // --- Private Fields ---
        private List<UIReward> _rewards = new();

        /// <summary>
        /// thong tin khi stage changed, type dungeon và hướng đi.
        /// 1: type dungeon
        /// 2: huong toi truoc hay sau
        /// </summary>
        private Func<int, int, IReadOnlyList<ItemRewardData>> _onStageChanged;

        private int _dungeonId;
        private Action<int, int> _onStart;

        private int _currentStageIdx;
        private int _maxStageIdx;

        private int ownedTicket;

        private void Awake()
        {
            btnNext.onClick.AddListener(OnClickNext);
            btnPrev.onClick.AddListener(OnClickPrev);
            btnStart.onClick.AddListener(OnClickStart);
            btnSweep.onClick.AddListener(() =>
            {
                UIManager.Instance.ShowToast("Coming Soon");
            });
            consecutiveChallengeToggle.onValueChanged.AddListener(_ =>
            {
                UIManager.Instance.ShowToast("Coming Soon");
            });
    
        }

        private void OnClickStart()
        {
            if(ownedTicket <= 0)
            {
                UIManager.Instance.ShowToast("Not Enough Dungeon Ticket");
                return;
            }

            _onStart?.Invoke(_dungeonId ,_currentStageIdx + 1);
            //for testing dungeon temporarily
            UIManager.Instance.TogglePopupAsync<DungeonView>();
            UIManager.Instance.TogglePopupAsync<DungeonMainView>();
        }

        private void OnClickNext()
        {
            OnStageChange(1);
        }

        private void OnStageChange(int direction)
        {
            _currentStageIdx = (Mathf.Max(0, _currentStageIdx + direction)) % _maxStageIdx;
            txtStage.SetText($"{_currentStageIdx + 1}");
            RefreshBtnDirection();

            if (_onStageChanged != null)
            {
                var rewards = _onStageChanged.Invoke(_dungeonId, _currentStageIdx + 1);
                RefreshRewards(rewards);
            }
        }

        private void OnClickPrev()
        {
            OnStageChange(-1);
        }

        private void RefreshBtnDirection()
        {
            if (_currentStageIdx >= _maxStageIdx)
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

        public void Bind(int dungeonId, int ticketOwned, int ticketRequired, string title, int currentStageIdx, int maxStageIdx, Action<int, int> onStart,
            Func<int, int, IReadOnlyList<ItemRewardData>> onStageChanged)
        {
            ownedTicket = ticketOwned;
            _dungeonId = dungeonId;
            _onStart = onStart;
            _currentStageIdx = currentStageIdx;
            _maxStageIdx = maxStageIdx;
            _onStageChanged = onStageChanged;

            txtTitle.SetText(title);
            txtTicket.SetText($"{ticketRequired}");
            txtTicketDungeonPlayer.SetText($"{ticketOwned}");
            txtHighestStage.SetText($"Cửa ải {_currentStageIdx + 1}");

            OnStageChange(0);
            RefreshBtnDirection();
        }

        private void RefreshRewards(IReadOnlyList<ItemRewardData> rewards)
        {
            for (var index = 0; index < rewards.Count; index++)
            {
                var entry = rewards[index];

                if (_rewards.Count > index)
                {
                    var clone = _rewards[index];
                    clone.gameObject.SetActive(true);
                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tierIcon);
                    clone.BindQuantity(entry.Quantity);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.Bind(entry.ItemIcon, entry.TierInfo.border, entry.TierInfo.background, entry.TierInfo.tierIcon);
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