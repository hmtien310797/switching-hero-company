using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [CreateAssetMenu(fileName = "WeaponLevelConfig", menuName = "ScriptableObjects/Equipment/WeaponLevelConfig")]
    public class WeaponLevelConfigSO : ScriptableObject
    {
        public string ConfigId;

        [Header("Formula")]
        [Min(0f)] public float BaseCost = 100f;
        [Min(0f)] public float Exponent = 1.2f;
        [Min(0f)] public float Multiplier = 1f;

        [Header("Clamp")]
        [Min(1)] public int MinCost = 1;

        public int GetCost(int nextLevel)
        {
            if (nextLevel <= 1)
                return 0;

            float raw = Multiplier * BaseCost * Mathf.Pow(nextLevel, Exponent);
            int result = Mathf.RoundToInt(raw);
            return Mathf.Max(MinCost, result);
        }
    }
}