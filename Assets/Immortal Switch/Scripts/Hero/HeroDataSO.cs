using Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "ScriptableObjects/HeroData", order = 1)]
    public class HeroDataSO : ScriptableObject
    {
        [Header("Identity")] 
        public int Id;
        public string Name;
        public HeroClass HeroClass;
        public Element Element;
        
        [Header("UI")]
        public Sprite PortraitIcon;
        public Sprite ShardIcon;

        [Header("Summon")] 
        public SummonRarity SummonRarity;
        public bool IsAvailableInSummon = true;
        [Min(1)] 
        public int SummonWeight = 1;

        [Header("Base Stats")] 
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

        [Header("Prefab")] 
        public PlayerHeroController PlayerHeroController;
    }

    public enum HeroClass
    {
        Warrior,
        Assassin,
        Archer,
        Mage
    }

    public enum SummonRarity
    {
        Common,
        UnCommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
    
    public enum HeroProgressTier
    {
        Common,
        UnCommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
}