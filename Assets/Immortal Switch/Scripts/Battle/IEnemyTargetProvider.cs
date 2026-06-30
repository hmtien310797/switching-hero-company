using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Battle
{
    public interface IEnemyTargetProvider
    {
        ICombatUnit GetNearestTarget(ICombatUnit requester);

        IReadOnlyList<ICombatUnit> GetAllTargets();

        bool IsValidTarget(ICombatUnit target);
    }
}