using System.Collections.Generic;
using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [CreateAssetMenu(fileName = "BossData", menuName = "ScriptableObjects/BossData", order = 1)]
    public class BossDataSO : ScriptableObject
    {
        public int Id;
        public string Name;
        public Element Element;
        public float BaseHP;
        public float BaseAtk;
        public float BaseDef;
        public float AtkSpeed;
        public float AttackRange;
        public float MoveSpeed;
        public MonsterBossController bossPrefab;
    }

    public enum StatType
    {
        HP,
        ATK,
        DEF,
        AtkSpeed,
        MoveSpeed,
        Evade
    }
}
