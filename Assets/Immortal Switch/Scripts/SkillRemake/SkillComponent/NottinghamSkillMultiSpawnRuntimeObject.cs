using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillRemake.SkillComponent
{
    public class NottinghamSkillMultiSpawnRuntimeObject : SkillMultiSpawnRuntimeObject
    {
        protected override Vector3 GetChildSpawnPosition(int _)
        {
            var currentEnemy = Context.BattleContext.GetRandomEnemyAlive();
            if (currentEnemy.IsUnityAlive())
            {
                return currentEnemy.Position;
            }

            return Vector3.zero;
        }
    }
}