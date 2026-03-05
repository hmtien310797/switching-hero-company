using System;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "HeroData", menuName = "ScriptableObjects/HeroData", order = 1)]
    public class HeroDataSO : ScriptableObject
    {
        public string Name;
        public HeroClass HeroClass;
        public Rarity Rarity;
        public float HitPoint;
        public float Attack;
        public float Defense;
        public float CritChance;
        public float CritDamage;
        public float AttackSpeed;
        public float AttackRange;
    }

    public enum HeroClass
    {
        Warrior, Assassin, Tank, Mage
    }
    
    public enum Rarity
    {
        Common, Rare, Epic, Legendary
    }
}