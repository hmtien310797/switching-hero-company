﻿using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.PowerUpSystem;
using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.StatSystem
{
    public class StatsController : MonoBehaviour
    {
        public StatModule StatModule { get; private set; }
        public HealthModule HealthModule { get; private set; }
        public StatusEffectModule StatusEffectModule { get; private set; }
        public BuffModule BuffModule { get; private set; }
        public DotModule DotModule { get; private set; }

        public void Initialize(BaseStat baseStat)
        {
            StatModule = new StatModule();
            DotModule = new DotModule();

            StatModule.Init(new Dictionary<StatType, float>
            {
                { StatType.MaxHp, baseStat.Health },
                { StatType.Atk, baseStat.Attack },
                { StatType.Def, baseStat.Defense },
                { StatType.AttackSpeed, baseStat.AttackSpeed },
                { StatType.AttackRange, baseStat.AttackRange },
                { StatType.CritChance, baseStat.CritChance },
                { StatType.CritDamage, baseStat.CritDamage },
                { StatType.Accuracy, baseStat.Accuracy },
                { StatType.DamageReduction, 0f },
                { StatType.DamageToNormalMonster, 0f },
                { StatType.DamageToHeroMonster, 0f },
                { StatType.ClassSkillDamage, 0f },
                { StatType.ExclusiveSkillDamage, 0f },
                { StatType.SwitchSkillDamage, 0f }
            });

            HealthModule = new HealthModule(StatModule);
            HealthModule.Init();

            StatusEffectModule = new StatusEffectModule();
            BuffModule = new BuffModule(StatModule, HealthModule, StatusEffectModule);

            BindDebug();
        }

        private void Update()
        {
            BuffModule?.Update(Time.deltaTime);
            DotModule?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.UnbindPlayer(this);
            }

            HealthModule?.Dispose();
        }

        private void BindDebug()
        {
            StatusEffectModule.OnStatusChanged += status =>
            {
                Debug.Log($"{name} Status => {status}");
            };

            HealthModule.OnDamaged += (value, type) =>
            {
                Debug.Log($"{name} Damaged => {value} ({type})");
            };

            HealthModule.OnHealed += value =>
            {
                Debug.Log($"{name} Healed => {value}");
            };

            BuffModule.OnBuffApplied += buff =>
            {
                Debug.Log($"{name} Buff Applied => {buff.Data.Name}");
            };

            BuffModule.OnBuffRemoved += buff =>
            {
                Debug.Log($"{name} Buff Removed => {buff.Data.Name}");
            };

            BuffModule.OnBuffTick += buff =>
            {
                Debug.Log($"{name} Buff Tick => {buff.Data.Name}");
            };
        }

        public bool CanMove()
        {
            return !StatusEffectModule.HasStatus(StatusEffectType.Stun)
                   && !StatusEffectModule.HasStatus(StatusEffectType.Freeze);
        }

        public bool CanCastSkill()
        {
            return !StatusEffectModule.HasStatus(StatusEffectType.Stun)
                   && !StatusEffectModule.HasStatus(StatusEffectType.Silence)
                   && !StatusEffectModule.HasStatus(StatusEffectType.Freeze);
        }

        public bool CanAttack()
        {
            return !StatusEffectModule.HasStatus(StatusEffectType.Stun)
                   && !StatusEffectModule.HasStatus(StatusEffectType.Freeze);
        }
    }
}