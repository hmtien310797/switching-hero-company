using System;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.DamageNumber;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonDefenseObjective : MonoBehaviour, ICombatUnit
    {
        [SerializeField] private StatsController stats;
        [SerializeField] private HealthBarController healthBarController;

        private bool destructionNotified;

        public event Action<DungeonDefenseObjective> OnDestroyed;

        public ActorType ActorType => ActorType.DefenseObjective;
        public StatsController Stats => stats;
        public Element Element => Element.None;
        public HealthBarController HealthBarController => healthBarController;
        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public bool IsDead => stats?.HealthModule == null || stats.HealthModule.IsDead;
        public float CurrentHp => stats?.HealthModule?.CurrentHP ?? 0f;
        public float MaxHp => stats?.HealthModule?.MaxHP ?? 0f;

        public void Initialize(BaseStat baseStat)
        {
            destructionNotified = false;

            if (stats == null)
                stats = GetComponent<StatsController>();

            if (stats == null || baseStat == null)
            {
                Debug.LogError("[DungeonDefenseObjective] Missing StatsController or BaseStat.");
                return;
            }

            stats.Initialize(baseStat);
            stats.HealthModule.OnDead -= HandleDead;
            stats.HealthModule.OnDead += HandleDead;
            healthBarController?.SetHealth(CurrentHp, MaxHp);
        }

        public DamageResult TakeDamage(DamageResult damageResult)
        {
            if (stats?.HealthModule == null || IsDead)
                return damageResult;

            stats.HealthModule.TakeDamage(damageResult);
            healthBarController?.SetHealth(CurrentHp, MaxHp);

            DamageNumberService.Instance?.ShowDamage(
                damageResult.Damage,
                Position,
                damageResult.DamageType
            );

            return damageResult;
        }

        public void Heal(float amount)
        {
            stats?.HealthModule?.ApplyHeal(amount);
            healthBarController?.SetHealth(CurrentHp, MaxHp);
        }

        private void HandleDead()
        {
            if (destructionNotified)
                return;

            destructionNotified = true;
            OnDestroyed?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (stats?.HealthModule != null)
                stats.HealthModule.OnDead -= HandleDead;
        }
    }
}
