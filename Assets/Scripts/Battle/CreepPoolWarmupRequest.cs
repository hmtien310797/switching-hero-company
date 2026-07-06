namespace Battle
{
    public readonly struct CreepPoolWarmupRequest
    {
        public int EnemyId { get; }
        public int WarmupCount { get; }

        public CreepPoolWarmupRequest(int enemyId, int warmupCount)
        {
            EnemyId = enemyId;
            WarmupCount = warmupCount;
        }
    }
}
