namespace Immortal_Switch.Scripts.Boss
{
    public static class BossSkillLogicFactory
    {
        public static IBossSkillLogic Create(int bossId)
        {
            return bossId switch
            {
                3001 => new Boss3001SkillLogic(),
                3002 => new Boss3002SkillLogic(),
                3003 => new Boss3003SkillLogic(),
                _ => new EmptyBossSkillLogic()
            };
        }
    }
}