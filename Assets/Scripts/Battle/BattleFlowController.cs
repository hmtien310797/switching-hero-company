using System;
using Battle.Dungeon;
using Cysharp.Threading.Tasks;
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
    public sealed class BattleFlowController : MonoBehaviour
    {
        [SerializeField] private PvEBattleController chapterBattleController;
        [SerializeField] private DungeonBattleController dungeonBattleController;
        [SerializeField, Min(0f)] private float resultHoldSeconds = 1f;

        public BattleFlowState State { get; private set; } = BattleFlowState.None;
        public bool IsDungeonLocked =>
            State == BattleFlowState.EnteringDungeon ||
            State == BattleFlowState.DungeonRunning ||
            State == BattleFlowState.ReturningToChapter;

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
                await chapterBattleController.SuspendForDungeonAsync();

                bool initialized = await dungeonBattleController.InitializeAsync(dungeonId, stage);
                if (!initialized)
                {
                    await chapterBattleController.ResumeAfterDungeonAsync();
                    State = BattleFlowState.ChapterRunning;
                    return false;
                }

                State = BattleFlowState.DungeonRunning;
                dungeonBattleController.BeginBattle();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                dungeonBattleController.CleanupBattle();
                await chapterBattleController.ResumeAfterDungeonAsync();
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
                Debug.Log($"[BattleFlow] Dungeon ended. Result={result}");

                if (resultHoldSeconds > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(resultHoldSeconds));

                dungeonBattleController.CleanupBattle();
                await chapterBattleController.ResumeAfterDungeonAsync();
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
    }
}
