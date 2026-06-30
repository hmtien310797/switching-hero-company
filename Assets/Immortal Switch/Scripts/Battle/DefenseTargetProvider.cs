using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.StatSystem;

namespace Battle
{
    /// <summary>
    /// Target provider cho Dungeon Defense.
    /// Normal target ưu tiên objective; danh sách AoE hero vẫn chỉ gồm các hero.
    /// </summary>
    public sealed class DefenseTargetProvider : IEnemyTargetProvider
    {
        private readonly Func<ICombatUnit> getObjective;
        private readonly HeroTargetProvider heroTargetProvider;

        public DefenseTargetProvider(
            Func<ICombatUnit> getObjective,
            Func<ICombatUnit> getHeroA,
            Func<ICombatUnit> getHeroB)
        {
            this.getObjective = getObjective;
            heroTargetProvider = new HeroTargetProvider(getHeroA, getHeroB);
        }

        public ICombatUnit GetNearestTarget(ICombatUnit requester)
        {
            ICombatUnit objective = getObjective?.Invoke();
            if (IsValidTarget(objective))
                return objective;

            return heroTargetProvider.GetNearestTarget(requester);
        }

        /// <summary>
        /// Giữ đúng ý nghĩa các skill hiện tại của boss: "all hero targets"
        /// chỉ trả các hero, không tự thêm Defense Objective.
        /// </summary>
        public IReadOnlyList<ICombatUnit> GetAllTargets()
        {
            return heroTargetProvider.GetAllTargets();
        }

        public bool IsValidTarget(ICombatUnit target)
        {
            return target.IsUnityAlive() && !target.IsDead;
        }
    }
}