using Battle;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "CreepData", menuName = "ScriptableObjects/CreepData", order = 1)]
    public class CreepDataSo : ScriptableObject
    {
        [field: SerializeField] public int Id { get; set; }
        [field: SerializeField] public int Name { get; set; }
        [field: SerializeField] public Element Element { get; set; }
        [field: SerializeField] public float BaseHp { get; set; }
        [field: SerializeField] public float BaseAtk { get; set; }
        [field: SerializeField] public float BaseDef { get; set; }
        [field: SerializeField] public float BaseAtkSpeed { get; set; }
        [field: SerializeField] public float BaseRange { get; set; }
        [field: SerializeField] public float BaseMoveSpeed { get; set; }
        [field: SerializeField] public EnemyActor CreepPrefab { get; set; }
    }
}
