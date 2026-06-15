using Battle;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "CreepData", menuName = "ScriptableObjects/CreepData", order = 1)]
    public class CreepDataSo : ScriptableObject
    {
        public int Id;
        public string Name;
        public string IconKey;
        public string CreepAddressKey;
        public Element Element;
        public float BaseHp;
        public float BaseAtk;
        public float BaseDef;
        public float BaseAtkSpeed;
        public float BaseRange;
        public float BaseMoveSpeed;
        public float BaseAccuracy;
    }
}
