using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventBingo.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Shared.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventBingo.Layout
{
    public class UIEventBingoLayoutController : MonoBehaviour
    {
        [Header("Component references")]
        [SerializeField]
        private UIEventBingoBoardPanel boardPanel;

        [SerializeField]
        private UIEventBingoProgressPanel progressPanel;

        [SerializeField]
        private UICountdownTimer countdownTimer;

        [Header("Button references")]
        [SerializeField]
        private Button btnDraw;

        [Header("Tile reveal")]
        [Min(0.02f)]
        [SerializeField]
        private float tileRevealDuration = 0.6f;

        [Header("Round transition")]
        [Min(0f)]
        [SerializeField]
        private float roundCompleteEffectDuration = 1.5f;

        // --- Private Fields ---
        private UIEventBingoLineChest[] _lineChests;

        private bool _isRevealingTile;

        private void Awake()
        {
            _lineChests = GetComponentsInChildren<UIEventBingoLineChest>(true);
            btnDraw.onClick.AddListener(OnClickDraw);
        }

        private void OnEnable()
        {
            EventBingoManager.Instance.OnDataChanged += RefreshStatePanels;
            EventBingoManager.Instance.OnRoundCompleted += HandleRoundCompleted;
        }

        private void OnDisable()
        {
            EventBingoManager.Instance.OnDataChanged -= RefreshStatePanels;
            EventBingoManager.Instance.OnRoundCompleted -= HandleRoundCompleted;
        }

        private void OnDestroy()
        {
            btnDraw.onClick.RemoveListener(OnClickDraw);
        }

        /// <summary>Khởi tạo countdown và hiển thị state Bingo Event hiện tại.</summary>
        public void Bind(double remainTime)
        {
            countdownTimer.Bind(remainTime, OnCountdown);
            RefreshStatePanels();
        }

        private void OnClickDraw()
        {
            DrawAsync().Forget();
        }

        private async UniTaskVoid DrawAsync()
        {
            if (_isRevealingTile)
            {
                return;
            }

            _isRevealingTile = true;
            btnDraw.interactable = false;

            var (trackIndex, reward, error) = await EventBingoManager.Instance.DrawAsync();

            if (reward == null)
            {
                _isRevealingTile = false;

                RefreshStatePanels();

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"[UIEventBingoLayoutController] Draw failed: {error}");
                }

                return;
            }

            try
            {
                var hasRevealed = await boardPanel.RevealTileAsync(
                    trackIndex,
                    tileRevealDuration,
                    this.GetCancellationTokenOnDestroy()
                );

                if (!hasRevealed)
                {
                    CompleteTileReveal(trackIndex);
                    return;
                }

                PopupRewardService.Show(
                    new List<ItemData> { reward, },
                    () => CompleteTileReveal(trackIndex)
                );
            }
            catch (OperationCanceledException)
            {
                _isRevealingTile = false;
            }
        }

        private string OnCountdown(long arg1, long arg2, long arg3, long arg4)
        {
            return $"Refresh time: {arg2:00}:{arg3:00}:{arg4:00}";
        }

        private void OnClaimMilestone(int milestoneId)
        {
            ClaimMilestoneAsync(milestoneId).Forget();
        }

        private async UniTaskVoid ClaimMilestoneAsync(int milestoneId)
        {
            var (rewards, error) = await EventBingoManager.Instance.ClaimMilestoneAsync(milestoneId);
            RefreshStatePanels();
            ShowClaimResult(rewards, error, $"milestone {milestoneId}");
        }

        private void OnClaimLineChest(EEventBingoLineId lineId)
        {
            ClaimLineChestAsync(lineId).Forget();
        }

        private async UniTaskVoid ClaimLineChestAsync(EEventBingoLineId lineId)
        {
            var (rewards, error, _) = await EventBingoManager.Instance.ClaimLineChestAsync(
                (int)lineId,
                TimeSpan.FromSeconds(roundCompleteEffectDuration),
                this.GetCancellationTokenOnDestroy()
            );

            RefreshStatePanels();
            ShowClaimResult(rewards, error, $"line chest {lineId}");
        }

        private void RefreshStatePanels()
        {
            if (_isRevealingTile)
            {
                return;
            }

            var manager = EventBingoManager.Instance;

            if (manager.State == null)
            {
                return;
            }

            var boardRows = DatabaseManager.Instance.GetEventBingoBoard(manager.CurrentPoolId);
            var milestones = DatabaseManager.Instance.GetEventBingoMilestone();

            boardPanel.Bind(boardRows, manager.GetUnlockedTrackIndices());

            progressPanel.Bind(
                milestones,
                manager.CurrentProgress,
                manager.GetClaimedMilestoneIds(),
                OnClaimMilestone
            );

            foreach (var lineChest in _lineChests)
            {
                var lineId = (int)lineChest.LineId;

                lineChest.Bind(
                    manager.IsLineUnlocked(lineId),
                    manager.IsLineChestClaimed(lineId),
                    OnClaimLineChest
                );
            }

            btnDraw.interactable = !manager.IsChangingRound &&
                                   boardRows.Any(row => !manager.IsTileUnlocked(row.trackIndex));
        }

        private void HandleRoundCompleted()
        {
            btnDraw.interactable = false;
        }

        private void CompleteTileReveal(int trackIndex)
        {
            if (this == null)
            {
                return;
            }

            boardPanel.SetTileClaimed(trackIndex, true);

            _isRevealingTile = false;

            RefreshStatePanels();
        }

        private static void ShowClaimResult(
            List<ItemData> rewards,
            string error,
            string source
        )
        {
            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(
                    $"[UIEventBingoLayoutController] Claim {source} failed: {error}"
                );
            }
        }
    }
}