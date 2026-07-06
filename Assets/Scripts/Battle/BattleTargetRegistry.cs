using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    public sealed class BattleTargetRegistry : IBattleTargetRegistry
    {
        private readonly List<ICombatUnit> hostileTargets = new(32);

        public IReadOnlyList<ICombatUnit> HostileTargets => hostileTargets;

        public void RegisterHostile(ICombatUnit target)
        {
            if (!IsValid(target) || hostileTargets.Contains(target))
                return;

            hostileTargets.Add(target);
        }

        public void UnregisterHostile(ICombatUnit target)
        {
            if (target == null)
                return;

            hostileTargets.Remove(target);
        }

        public ICombatUnit GetNearestHostile(Vector3 position)
        {
            ICombatUnit nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = hostileTargets.Count - 1; i >= 0; i--)
            {
                ICombatUnit target = hostileTargets[i];

                if (!IsValid(target))
                {
                    hostileTargets.RemoveAt(i);
                    continue;
                }

                Vector3 targetPosition = target.Position;
                targetPosition.y = position.y;

                float sqrDistance = (targetPosition - position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearest = target;
            }

            return nearest;
        }

        public void GetHostilesInRange(Vector3 center, float range, List<ICombatUnit> results)
        {
            if (results == null)
                return;

            results.Clear();
            float sqrRange = Mathf.Max(0f, range) * Mathf.Max(0f, range);

            for (int i = hostileTargets.Count - 1; i >= 0; i--)
            {
                ICombatUnit target = hostileTargets[i];

                if (!IsValid(target))
                {
                    hostileTargets.RemoveAt(i);
                    continue;
                }

                Vector3 targetPosition = target.Position;
                targetPosition.y = center.y;

                if ((targetPosition - center).sqrMagnitude <= sqrRange)
                    results.Add(target);
            }
        }

        public void Clear()
        {
            hostileTargets.Clear();
        }

        private static bool IsValid(ICombatUnit target)
        {
            return target.IsUnityAlive() && !target.IsDead;
        }
    }
}
