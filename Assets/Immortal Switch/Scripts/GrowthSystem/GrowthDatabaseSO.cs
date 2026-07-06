using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    [CreateAssetMenu(menuName = "Growth/Growth Database")]
    public class GrowthDatabaseSO : ScriptableObject
    {
        public GrowthDataSO[] Tiers;
    }
}