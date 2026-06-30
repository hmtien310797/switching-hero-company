using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle
{
    public sealed class HeroTargetProvider : IEnemyTargetProvider
    {
        private readonly Func<ICombatUnit> getHeroA;
        private readonly Func<ICombatUnit> getHeroB;
        private readonly List<ICombatUnit> targets = new(2);

        public HeroTargetProvider(
            Func<ICombatUnit> getHeroA,
            Func<ICombatUnit> getHeroB)
        {
            this.getHeroA = getHeroA;
            this.getHeroB = getHeroB;
        }

        public ICombatUnit GetNearestTarget(ICombatUnit requester)
        {
            RefreshTargets();

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
            RefreshTargets();
            return targets;
        }

        public bool IsValidTarget(ICombatUnit target)
        {
            return target.IsUnityAlive() && !target.IsDead;
        }

        private void RefreshTargets()
        {
            targets.Clear();

            ICombatUnit heroA = getHeroA?.Invoke();
            if (IsValidTarget(heroA))
                targets.Add(heroA);

            ICombatUnit heroB = getHeroB?.Invoke();
            if (IsValidTarget(heroB))
                targets.Add(heroB);
        }
    }
}
