using System;
using System.Threading;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.DungeonSystem;
using Nakama;
using UnityEngine;

namespace Battle
{
    public enum BattleFlowState
    {
        None = 0,
        ChapterRunning = 1,
        EnteringDungeon = 2,
        DungeonRunning = 3,
        ReturningToChapter = 4
    }

    /// <summary>
    /// Điều phối Chapter và Dungeon trong cùng scene.
    /// Không expose API thoát Dungeon giữa trận.
    /// </summary>
    public sealed class BattleFlowController : Singleton<BattleFlowController>
    {
        [SerializeField] private PvEBattleController chapterBattleController;
        [SerializeField] private DungeonBattleController dungeonBattleController;
        [SerializeField, Min(0f)] private float resultHoldSeconds = 1f;

        public BattleFlowState State { get; private set; } = BattleFlowState.None;
        public CancellationTokenSource stageFlowCancellationTokenSource;
        public CancellationTokenSource endStageSessionCancellationTokenSource;
        public bool IsDungeonLocked =>
            State == BattleFlowState.EnteringDungeon ||
            State == BattleFlowState.DungeonRunning ||
            State == BattleFlowState.ReturningToChapter;

        /// <summary>
        /// Fires after every dungeon/end call (Success = true hoặc false) — hook cho UI hiển thị
        /// popup thưởng / toast lỗi (STAGE_LOCKED, INSUFFICIENT_TICKET, ...). BattleFlowController
        /// chỉ gửi dữ liệu về, không tự hiển thị gì — UI team tự thêm subscriber khi cần.
        /// </summary>
        public event Action<DungeonEndResponse> DungeonEndReported;

        private bool transitionInProgress;

        private void OnEnable()
        {
            if (dungeonBattleController != null)
                dungeonBattleController.BattleEnded += OnDungeonBattleEnded;
        }

        private void OnDisable()
        {
            if (dungeonBattleController != null)
                dungeonBattleController.BattleEnded -= OnDungeonBattleEnded;
        }

        private void Start()
        {
            CreateDungeonBattleCancellationToken();
            CreateEndStageSessionCancellationToken();
            GameEventManager.Subscribe(GameEvents.OnStageSessionChange, CreateEndStageSessionCancellationToken);
            GameEventManager.Subscribe(
                GameEvents.OnSelectedDungeonStage,
                (Action<int, int>)StartDungeonFlow
            );
        }

        protected override void OnDestroy()
        {
            CancelEndStageSessionCancellationToken();
            CancelDungeonBattleCancellationToken();
            GameEventManager.Unsubscribe(GameEvents.OnStageSessionChange, CreateEndStageSessionCancellationToken);
            GameEventManager.Unsubscribe(GameEvents.OnSelectedDungeonStage, (Action<int, int>)StartDungeonFlow);
            base.OnDestroy();
        }

        private void CreateDungeonBattleCancellationToken()
        {
            CancelDungeonBattleCancellationToken();

            stageFlowCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    this.GetCancellationTokenOnDestroy()
                );
        }
        
        private void CreateEndStageSessionCancellationToken()
        {
            CancelEndStageSessionCancellationToken();

            if (stageFlowCancellationTokenSource != null &&
                !stageFlowCancellationTokenSource.IsCancellationRequested)
            {
                // Dungeon mode:
                // endStageSession là con của stageFlow
                endStageSessionCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        this.GetCancellationTokenOnDestroy(),
                        stageFlowCancellationTokenSource.Token
                    );
            }
            else
            {
                // Chapter thường:
                // endStageSession độc lập
                endStageSessionCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        this.GetCancellationTokenOnDestroy()
                    );
            }
        }

        private void CancelDungeonBattleCancellationToken()
        {
            if (stageFlowCancellationTokenSource == null)
                return;

            stageFlowCancellationTokenSource.Cancel();
            stageFlowCancellationTokenSource.Dispose();
            stageFlowCancellationTokenSource = null;
        }
        
        private void CancelEndStageSessionCancellationToken()
        {
            if (endStageSessionCancellationTokenSource == null)
                return;

            if (!endStageSessionCancellationTokenSource.IsCancellationRequested)
            {
                endStageSessionCancellationTokenSource.Cancel();
            }

            endStageSessionCancellationTokenSource.Dispose();
            endStageSessionCancellationTokenSource = null;
        }
        
        private void StartDungeonFlow(int dungeonId, int stage)
        {
            CancelEndStageSessionCancellationToken();
            CancelDungeonBattleCancellationToken();
            EnterDungeonAsync(dungeonId,stage).Forget();
        }
        

        public void MarkChapterRunning()
        {
            if (State == BattleFlowState.None)
                State = BattleFlowState.ChapterRunning;
        }
        
        public async UniTask<bool> EnterDungeonAsync(int dungeonId, int stage)
        {
            if (transitionInProgress || State != BattleFlowState.ChapterRunning)
                return false;

            if (chapterBattleController == null || dungeonBattleController == null)
            {
                Debug.LogError("[BattleFlow] Missing Chapter or Dungeon controller.");
                return false;
            }

            transitionInProgress = true;
            State = BattleFlowState.EnteringDungeon;

            try
            {
                await Transitioner.Instance.TransitionOutWithoutChangingScene(destroyCancellationToken);
                chapterBattleController.SuspendForDungeon();

                bool initialized = await dungeonBattleController.InitializeAsync(dungeonId, stage);
                if (!initialized)
                {
                    await chapterBattleController.ResumeAfterDungeonAsync();
                    Transitioner.Instance.TransitionInWithoutChangingScene();
                    State = BattleFlowState.ChapterRunning;
                    return false;
                }
                GameEventManager.Trigger(GameEvents.OnPlayDungeon, true);
                Transitioner.Instance.TransitionInWithoutChangingScene();

                State = BattleFlowState.DungeonRunning;
                dungeonBattleController.BeginBattle();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                dungeonBattleController.CleanupBattle();


                await chapterBattleController.ResumeAfterDungeonAsync();
                Transitioner.Instance.TransitionInWithoutChangingScene();
                State = BattleFlowState.ChapterRunning;
                return false;
            }
            finally
            {
                transitionInProgress = false;
            }
        }

        private void OnDungeonBattleEnded(DungeonBattleResult result)
        {
            if (State != BattleFlowState.DungeonRunning)
                return;

            ReturnToChapterAfterResultAsync(result).Forget();
        }

        private async UniTaskVoid ReturnToChapterAfterResultAsync(DungeonBattleResult result)
        {
            if (transitionInProgress)
                return;

            transitionInProgress = true;
            State = BattleFlowState.ReturningToChapter;

            try
            {
                CreateDungeonBattleCancellationToken();
                CreateEndStageSessionCancellationToken();
                Debug.Log($"[BattleFlow] Dungeon ended. Result={result}");

                // Capture before CleanupBattle() wipes RuntimeData below.
                var runtimeData = dungeonBattleController.RuntimeData;
                if (runtimeData != null && result != DungeonBattleResult.None)
                    await ReportDungeonEndAsync(runtimeData.DungeonKey, runtimeData.Stage, result);

                if (resultHoldSeconds > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(resultHoldSeconds));
                
                await Transitioner.Instance.TransitionOutWithoutChangingScene(destroyCancellationToken);
                dungeonBattleController.CleanupBattle();

                await chapterBattleController.ResumeAfterDungeonAsync();
                Transitioner.Instance.TransitionInWithoutChangingScene();
                GameEventManager.Trigger(GameEvents.OnPlayDungeon, false);
                State = BattleFlowState.ChapterRunning;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                State = BattleFlowState.None;
            }
            finally
            {
                transitionInProgress = false;
            }
        }

        /// <summary>
        /// Báo kết quả trận Dungeon lên server — server trừ vé (dù thắng hay thua) + tính
        /// reward + cập nhật highest_stage_cleared trong 1 lần gọi (không có RPC "enter" riêng).
        /// Client không tự trừ vé/cộng thưởng cho dungeon — CurrencyManager chỉ ghi đè theo
        /// response.Balances (RewardDto[], absolute), không dùng response.Rewards (delta) trực tiếp.
        /// </summary>
        private async UniTask ReportDungeonEndAsync(string dungeonKey, int stage, DungeonBattleResult result)
        {
            DungeonEndResponse res;
            try
            {
                res = await NakamaClient.Instance.DungeonEndAsync(new DungeonEndRequest
                {
                    DungeonKey = dungeonKey,
                    Stage      = stage,
                    Result     = result.ToString() // enum values are literally "Victory"/"Defeat"
                });
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[BattleFlow] dungeon/end failed: {ex.StatusCode} {ex.Message}");
                return;
            }

            if (res.Success)
            {
                CurrencyManager.Instance?.ApplyServerBalances(res.Balances);
            }
            else
            {
                // STAGE_LOCKED / INSUFFICIENT_TICKET / INVALID_DUNGEON / INVALID_STAGE — no local
                // state to roll back (dungeon runs are 100% local combat with no client-side
                // ticket/stage tracking today), just log for now.
                Debug.LogWarning($"[BattleFlow] dungeon/end business error: {res.Error}");
            }

            DungeonEndReported?.Invoke(res);
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
