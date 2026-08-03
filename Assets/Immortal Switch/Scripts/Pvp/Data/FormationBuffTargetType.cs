namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Target resolver của buff effect (DOCX §18 — "Target" examples: Owner, Ally, EnemyFront,
    /// EnemyBack, AllEnemies, LowestHpEnemy).
    /// </summary>
    public enum FormationBuffTargetType
    {
        Owner = 0,
        Ally = 1,
        EnemyFront = 2,
        EnemyBack = 3,
        AllEnemies = 4,
        LowestHpEnemy = 5
    }
}
