using Immortal_Switch.Scripts;
using Scripts.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Hero
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "ScriptableObjects/HeroData", order = 1)]
    public class HeroDataSO : ScriptableObject
    {
        [Header("Identity")] public int Id;
        public string Name;
        public HeroClass HeroClass;
        public Element Element;

        [Header("Summon")] public SummonRarity SummonRarity;
        public bool IsAvailableInSummon = true;
        [Min(1)] public int SummonWeight = 1;

        [Header("Base Stats")] [FormerlySerializedAs("HitPoint")]
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

        [Header("Prefab")] public PlayerHeroController PlayerHeroController;
    }

    public enum HeroClass
    {
        Warrior,
        Assassin,
        Tank,
        Mage
    }

    public enum SummonRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Legendary1,
        Legendary2
    }
    
    public enum HeroProgressTier
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Legendary1,
        Legendary2
    }
}