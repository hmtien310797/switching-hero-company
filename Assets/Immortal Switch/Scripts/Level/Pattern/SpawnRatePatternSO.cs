using Unity.Mathematics;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "SpawnRatePattern", menuName = "ScriptableObjects/SpawnRatePattern")]
    public class SpawnRatePatternSO : ScriptableObject
    {
        public SpawnRatePattern[] SpawnRatePatterns;
    }

    [System.Serializable]
    public struct SpawnRatePattern
    {
        public int Id;
        public float[] SpawnRate;
    }
}