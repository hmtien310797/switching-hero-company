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
        BossActor GetActiveBossActor();
        void OnSelectedHeroCastUltimateSkill();
    }
}