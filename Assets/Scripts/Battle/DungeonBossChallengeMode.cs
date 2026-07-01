using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;

namespace Battle.Dungeon
{
    public sealed class DungeonBossChallengeMode : IDungeonMode
    {
        public DungeonModeType ModeType => DungeonModeType.BossChallenge;

        private DungeonModeContext context;
        private BossActor boss;
        private bool active;

        public UniTask InitializeAsync(DungeonModeContext context)
        {
            this.context = context;
            boss = null;
            active = false;
            return UniTask.CompletedTask;
        }

        public void Begin()
        {
            active = true;
            SpawnBossAsync().Forget();
        }

        public void Tick(float deltaTime)
        {
        }

        public void NotifyBossDead(BossActor deadBoss)
        {
            if (!active)
                return;

            active = false;
            boss = null;
            context.ScoreChanged?.Invoke(1);
            context.Victory?.Invoke();
        }

        public void OnTimeExpired()
        {
            if (!active)
                return;

            active = false;
            context.Defeat?.Invoke();
        }

        public void Dispose()
        {
            active = false;
            boss = null;
            context = null;
        }

        private async UniTaskVoid SpawnBossAsync()
        {
            if (context == null || context.RuntimeData.BossId <= 0)
            {
                context?.Defeat?.Invoke();
                return;
            }

            boss = await context.SpawnBossAsync(context.RuntimeData.BossId);
            if (boss == null)
            {
                active = false;
                context.Defeat?.Invoke();
            }
        }
    }
}
