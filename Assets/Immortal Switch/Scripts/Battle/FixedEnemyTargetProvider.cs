using System.Collections.Generic;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    /// <summary>
    /// Compatibility provider for existing callers that still pass heroA/heroB directly.
    /// New battle controllers should prefer a dynamic provider such as HeroTargetProvider.
    /// </summary>
    public sealed class FixedEnemyTargetProvider : IEnemyTargetProvider
    {
        private readonly List<ICombatUnit> targets = new(2);

        public FixedEnemyTargetProvider(
            ICombatUnit targetA,
            ICombatUnit targetB)
        {
            SetTargets(targetA, targetB);
        }

        public void SetTargets(
            ICombatUnit targetA,
            ICombatUnit targetB)
        {
            targets.Clear();

            if (targetA.IsUnityAlive())
                targets.Add(targetA);

            if (targetB.IsUnityAlive())
                targets.Add(targetB);
        }

        public ICombatUnit GetNearestTarget(ICombatUnit requester)
        {
            if (!requester.IsUnityAlive())
                return null;

            ICombatUnit nearestTarget = null;
            float nearestDistanceSqr = float.MaxValue;

            Vector3 requesterPosition = requester.Position;
            requesterPosition.y = 0f;

            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (!IsValidTarget(target))
                    continue;

                Vector3 targetPosition = target.Position;
                targetPosition.y = 0f;

                float distanceSqr =
                    (targetPosition - requesterPosition).sqrMagnitude;

                if (distanceSqr >= nearestDistanceSqr)
                    continue;

                nearestDistanceSqr = distanceSqr;
                nearestTarget = target;
            }

            return nearestTarget;
        }

        public IReadOnlyList<ICombatUnit> GetAllTargets()
        {
            return targets;
        }

        public bool IsValidTarget(ICombatUnit target)
        {
            return target.IsUnityAlive() && !target.IsDead;
        }
    }
}
