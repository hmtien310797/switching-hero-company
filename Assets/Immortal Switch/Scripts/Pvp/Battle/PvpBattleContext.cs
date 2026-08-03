using System;
using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// IHeroBattleContext cho 1 team PvP. <c>TargetRegistry</c> = OPPOSING team's registry (enemies).
    /// <c>GetAllies</c> (IAllyProvider) = own team heroes. <c>GetNearestEnemy</c> /
    /// <c>GetRandomEnemyAlive</c> over opposing registry. <c>ActiveEnemies</c> /
    /// <c>GetActiveBossActor</c> stub (PvE-only — không dùng trong PvP). FLAGGED: PvP-specific.
    /// </summary>
    public sealed class PvpBattleContext : IHeroBattleContext, IAllyProvider
    {
        private readonly List<ICombatUnit> _allies;             // own team heroes (ref to team list)
        private readonly IBattleTargetRegistry _enemyRegistry; // opposing team's registry
        private readonly IPvPBattleRandomService _rng;

        public PvpBattleContext(List<ICombatUnit> allies, IBattleTargetRegistry enemyRegistry, IPvPBattleRandomService rng)
        {
            _allies = allies;
            _enemyRegistry = enemyRegistry;
            _rng = rng;
        }

        public IBattleTargetRegistry TargetRegistry => _enemyRegistry;

        public IReadOnlyList<EnemyActor> ActiveEnemies => Array.Empty<EnemyActor>();

        public BossActor GetActiveBossActor() => null;

        public void OnSelectedHeroCastUltimateSkill() { }

        public ICombatUnit GetNearestEnemy(Vector3 position)
        {
            var nearest = _enemyRegistry != null ? _enemyRegistry.GetNearestHostile(position) : null;

            // [DEBUG] trace target resolution.
            var hostiles = _enemyRegistry?.HostileTargets;
            int registered = hostiles != null ? hostiles.Count : 0;
            int alive = 0;
            if (hostiles != null)
            {
                for (int i = 0; i < hostiles.Count; i++)
                {
                    var t = hostiles[i];
                    if (t != null && !t.IsDead) alive++;
                }
            }
            Debug.Log($"[PvP] GetNearestEnemy pos={position} registered={registered} alive={alive} nearest={(nearest != null ? "FOUND" : "NULL")}");

            return nearest;
        }

        public ICombatUnit GetRandomEnemyAlive()
        {
            if (_enemyRegistry == null) return null;
            var hostiles = _enemyRegistry.HostileTargets;
            if (hostiles == null || hostiles.Count == 0) return null;

            ICombatUnit picked = null;
            int count = 0;
            for (int i = 0; i < hostiles.Count; i++)
            {
                var t = hostiles[i];
                if (t == null || t.IsDead) continue;
                count++;
                // Reservoir-style pick để tránh alloc List (deterministic với seeded rng).
                int idx = _rng != null ? _rng.Range(0, count) : UnityEngine.Random.Range(0, count);
                if (idx == 0) picked = t;
            }
            return picked;
        }

        public ICombatUnit GetRandomFromFarthestEnemies(Vector3 position, IReadOnlyList<ICombatUnit> excludedTargets, int topCount = 5)
        {
            // Simplified: random alive enemy (farthest heuristic là PvE-specific; PvP front-first qua SkillResolver).
            return GetRandomEnemyAlive();
        }

        // IAllyProvider — own team heroes (ResolveAllAllies / ResolveAllyTargets dùng cái này).
        public IReadOnlyList<ICombatUnit> GetAllies() => (IReadOnlyList<ICombatUnit>)_allies ?? Array.Empty<ICombatUnit>();
    }
}
