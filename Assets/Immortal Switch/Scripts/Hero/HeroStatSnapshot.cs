using System;

namespace Immortal_Switch.Scripts.Hero
{
    [Serializable]
    public class HeroStatSnapshot
    {
        public int HeroId;
        public HeroProgressTier CurrentTier;
        public int CurrentStarInTier;
        public int CurrentShard;

        public float Health;
        public float IdleStateTime;
        public float IdleIntervalTime;
        public float AttackRange;
        public float Defense;
        public float Attack;
        public float AttackSpeed;
        public float CritChance;
        public float CritDamage;
        public float Accuracy;
        public float MoveSpeed;
    }
}