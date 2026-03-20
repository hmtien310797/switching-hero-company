using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts
{
    [CreateAssetMenu(fileName = "EnemySpawnPattern", menuName = "ScriptableObjects/EnemySpawnPattern")]
    public class CreepSpawnPatternCollectionSO : ScriptableObject
    {
        public CreepSpawnPattern[] ListSpawnPattern;
        
        public int[] GetSpawnPatternBaseOnId(int id)
        {
            for (int i = 0; i < ListSpawnPattern.Length; i++)
            {
                var spawnPattern = ListSpawnPattern[i];
                if (spawnPattern.Id == id)
                {
                    return spawnPattern.ListEnemyId;
                }
            }

            return null;
        }
    }

    [System.Serializable]
    public struct CreepSpawnPattern
    {
        public int Id;
        public int[] ListEnemyId;
    }
    
}