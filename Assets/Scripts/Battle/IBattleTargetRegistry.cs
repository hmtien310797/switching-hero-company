using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    public interface IBattleTargetRegistry
    {
        IReadOnlyList<ICombatUnit> HostileTargets { get; }

        void RegisterHostile(ICombatUnit target);
        void UnregisterHostile(ICombatUnit target);
        ICombatUnit GetNearestHostile(Vector3 position);
        void GetHostilesInRange(Vector3 center, float range, List<ICombatUnit> results);
        void Clear();
    }
}
