using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventDice.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventDice.Layout
{
    public class UIEventDiceLayoutController : MonoBehaviour
    {
        [Header("Component references")]
        [SerializeField]
        private UIEventDiceRollPanel diceRollPanel;

        [SerializeField]
        private UIEventDiceBoardPanel boardPanel;

        [SerializeField]
        private UIEventDiceProgressPanel progressPanel;

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventDiceBoardRow> boardRows,
            List<ItemData> pendingRewards,
            int currentIndex,
            Func<CancellationToken, UniTask<int>> onStartRoll
        )
        {
            diceRollPanel.Bind(
                pendingRewards,
                onStartRoll,
                OnRollCompletedAsync,
                OnClaimPendingRewards
            );

            boardPanel.Bind(boardRows, currentIndex);
            RefreshStatePanels();
        }

        /// <summary>Chờ flag di chuyển xong rồi refresh UI từ service state.</summary>
        private async UniTask OnRollCompletedAsync(int result)
        {
            await boardPanel.MoveFlagAsync(result);
            RefreshStatePanels();
        }

        private void OnClaimMilestone(int milestoneId)
        {
            ClaimMilestoneAsync(milestoneId).Forget();
        }

        private async UniTaskVoid ClaimMilestoneAsync(int milestoneId)
        {
            var milestones = DatabaseManager.Instance.GetEventDiceMilestone();
            var milestone = milestones.FirstOrDefault(row => row.milestone == milestoneId);
            var (rewards, error) = await EventDiceManager.Instance.ClaimMilestoneAsync(milestone);

            RefreshStatePanels();

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(
                    $"[UIEventDiceLayoutController] Claim milestone {milestoneId} failed: {error}"
                );
            }
        }

        private void OnClaimPendingRewards()
        {
            ClaimPendingRewardsAsync().Forget();
        }

        private async UniTaskVoid ClaimPendingRewardsAsync()
        {
            var rewards = await EventDiceManager.Instance.ClaimPendingRewardsAsync();
            RefreshStatePanels();

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void RefreshStatePanels()
        {
            var manager = EventDiceManager.Instance;
            var milestones = DatabaseManager.Instance.GetEventDiceMilestone();

            diceRollPanel.RefreshPendingRewards(manager.GetPendingRewards());

            progressPanel.Bind(
                milestones,
                manager.CurrentProgress,
                manager.GetClaimedMilestoneIds(),
                OnClaimMilestone
            );
        }
    }
}