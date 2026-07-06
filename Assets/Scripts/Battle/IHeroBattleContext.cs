using System.Collections.Generic;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    public interface IHeroBattleContext
    {
        IBattleTargetRegistry TargetRegistry { get; }
        IReadOnlyList<EnemyActor> ActiveEnemies { get; }
        ICombatUnit GetNearestEnemy(Vector3 position);
        ICombatUnit GetRandomEnemyAlive();
        ICombatUnit GetRandomFromFarthestEnemies(
            Vector3 position,
            IReadOnlyList<ICombatUnit> excludedTargets,
            int topCount = 5);
        BossActor GetActiveBossActor();
        void OnSelectedHeroCastUltimateSkill();
    }
}