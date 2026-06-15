using Battle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "ScriptableObjects/HeroData", order = 1)]
    public class HeroDataSO : ScriptableObject
    {
        [Header("Identity")] 
        public int Id;
        public string Name;
        
        [Header("Base Stats")] 
        public float Health;
        public float AttackRange;
        public float Defense;
        public float Attack;
        public float AttackSpeed;
        public float CritChance;
        public float CritDamage;
        public float Accuracy;
        public HeroClass HeroClass;
        public Element Element;
        
        [Header("UI")]
        [PreviewField] public Sprite PortraitIcon;
        [PreviewField] public Sprite ShardIcon;

        [Header("Summon")] 
        public SummonRarity SummonRarity;
        public bool IsAvailableInSummon = true;
        [Min(1)] 
        public int SummonWeight = 1;
        public string HeroAddressKey;
        public string SpineAddressKey;
        public string HeroIconKey;
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