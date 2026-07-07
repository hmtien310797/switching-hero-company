using Cysharp.Threading.Tasks;

namespace Battle.Dungeon
{
    public sealed class DungeonDamageChallengeMode : IDungeonMode
    {
        public DungeonModeType ModeType => DungeonModeType.DamageChallenge;

        private DungeonModeContext context;
        private DungeonDamageDummy dummy;
        private bool active;

        public UniTask InitializeAsync(DungeonModeContext context)
        {
            this.context = context;
            dummy = null;
            active = false;
            return UniTask.CompletedTask;
        }

        public void Begin()
        {
            if (context?.RuntimeData?.DamageChallenge == null)
            {
                context?.Defeat?.Invoke();
                return;
            }

            dummy = context.SpawnDamageDummy?.Invoke();

            if (dummy == null)
            {
                context.Defeat?.Invoke();
                return;
            }

            dummy.OnTotalDamageChanged -= HandleDamageChanged;
            dummy.OnTotalDamageChanged += HandleDamageChanged;
            active = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public void OnTimeExpired()
        {
            if (!active || context?.RuntimeData?.DamageChallenge == null)
            {
                return;
            }

            active = false;

            double totalDamage = dummy != null
                ? dummy.TotalDamage
                : 0d;

            context.ScoreChanged?.Invoke(ToLongScore(totalDamage));

            if (context.RuntimeData.DamageChallenge.IsCleared(totalDamage))
            {
                context.Victory?.Invoke();
            }
            else
            {
                context.Defeat?.Invoke();
            }
        }

        public void Dispose()
        {
            active = false;

            if (dummy != null)
            {
                dummy.OnTotalDamageChanged -= HandleDamageChanged;
            }

            dummy = null;
            context = null;
        }

        private void HandleDamageChanged(double totalDamage)
        {
            context?.ScoreChanged?.Invoke(ToLongScore(totalDamage));
        }

        private static long ToLongScore(double value)
        {
            if (value <= 0d)
            {
                return 0L;
            }

            if (value >= long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)value;
        }
    }
}
