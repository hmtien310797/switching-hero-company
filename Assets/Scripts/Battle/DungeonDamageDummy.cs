using System;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.DamageNumber;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;
using Immortal_Switch.Scripts;

namespace Battle.Dungeon
{
    public sealed class DungeonDamageDummy : MonoBehaviour, ICombatUnit
    {
        [SerializeField] private StatsController stats;
        [SerializeField] private HealthBarController healthBarController;

        private double totalDamage;

        public event Action<double> OnTotalDamageChanged;

        public ActorType ActorType => ActorType.DamageDummy;
        public StatsController Stats => stats;
        public Element Element => Element.None;
        public HealthBarController HealthBarController => healthBarController;
        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public bool IsDead => false;
        public float CurrentHp => MaxHp;
        public float MaxHp => stats?.HealthModule?.MaxHP ?? 1f;
        public double TotalDamage => totalDamage;

        public void Initialize(BaseStat baseStat)
        {
            totalDamage = 0d;

            if (stats == null)
                stats = GetComponent<StatsController>();

            if (stats != null && baseStat != null)
                stats.Initialize(baseStat);

            healthBarController?.SetHealth(CurrentHp, MaxHp);
        }

        public DamageResult TakeDamage(DamageResult damageResult)
        {
            float damage = Mathf.Max(0f, damageResult.Damage);
            totalDamage += damage;

            DamageNumberService.Instance?.ShowDamage(
                damage,
                Position,
                damageResult.DamageType
            );

            OnTotalDamageChanged?.Invoke(totalDamage);
            return damageResult;
        }

        public void Heal(float amount)
        {
        }
    }
}
