using Scripts.Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "ScriptableObjects/HeroData", order = 1)]
    public class HeroDataSO : ScriptableObject
    {
        public int Id;
        public string Name;
        public HeroClass HeroClass;
        public Rarity Rarity;
        public Element Element;
        [FormerlySerializedAs("HitPoint")] 
        public float Health;
        public float Attack;
        public float Defense;
        public float CritChance;
        public float CritDamage;
        public float AttackSpeed;
        public float AttackRange;
        public PlayerHeroController PlayerHeroController;
    }

    public enum HeroClass
    {
        Warrior, Assassin, Tank, Mage
    }
    
    public enum Rarity
    {
        Common, Rare, Epic, Legendary, Legendary1, Legendary2
    }
}