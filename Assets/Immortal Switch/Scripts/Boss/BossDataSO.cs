using UnityEngine;

namespace Immortal_Switch.Scripts.Boss
{
    [CreateAssetMenu(fileName = "BossData", menuName = "ScriptableObjects/BossData", order = 1)]
    public class BossDataSO : ScriptableObject
    {
        public int Id;
        public string Name;
        public Element Element;
        public string IconKey;
        public Sprite Icon;
        public float BaseHP;
        public float BaseAtk;
        public float BaseDef;
        public float AtkSpeed;
        public float AttackRange;
        public float MoveSpeed;
        public string BossAddressKey;
    }
}
