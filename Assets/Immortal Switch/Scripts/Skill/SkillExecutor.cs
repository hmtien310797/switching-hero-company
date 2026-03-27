using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Skill;
using Scripts.Battle;
using UnityEngine;

public static class SkillExecutor
{
    public static void ExecutePhase(PlayerHeroController caster, SkillPhaseData phase)
    {
        if (caster == null || phase == null || phase.Effects == null || phase.Effects.Count == 0)
            return;

        var targets = ResolveTargets(caster, phase.TargetTypeOverride);

        if (targets == null || targets.Count == 0)
            return;

        for (int i = 0; i < phase.Effects.Count; i++)
        {
            ExecuteEffect(caster, targets, phase.Effects[i]);
        }
    }

    public static void ExecuteEffect(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
    {
        if (caster == null || effect == null || targets == null || targets.Count == 0)
            return;

        if (!RollChance(effect.ChancePercent))
            return;

        switch (effect.EffectType)
        {
            case SkillEffectType.Damage:
                ExecuteDamage(caster, targets, effect);
                break;

            case SkillEffectType.ModifyStatPercent:
                ExecuteModifyStatPercent(caster, targets, effect);
                break;

            case SkillEffectType.ApplyStatus:
                ExecuteApplyStatus(caster, targets, effect);
                break;

            case SkillEffectType.HealPercentMaxHp:
                ExecuteHealPercentMaxHp(caster, effect);
                break;

            case SkillEffectType.ShieldPercentMaxHp:
                Debug.LogWarning("SkillExecutor: ShieldPercentMaxHp chưa implement.");
                break;

            case SkillEffectType.ReflectDamagePercent:
                Debug.LogWarning("SkillExecutor: ReflectDamagePercent chưa implement.");
                break;

            case SkillEffectType.DamageReductionPercent:
                Debug.LogWarning("SkillExecutor: DamageReductionPercent chưa implement.");
                break;

            case SkillEffectType.AddMark:
                Debug.LogWarning("SkillExecutor: AddMark chưa implement.");
                break;

            case SkillEffectType.TeleportToTarget:
                Debug.LogWarning("SkillExecutor: TeleportToTarget chưa implement.");
                break;

            default:
                Debug.LogWarning($"SkillExecutor: Chưa xử lý EffectType {effect.EffectType}");
                break;
        }
    }

    private static void ExecuteDamage(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
    {
        float factor = effect.DamageMultiplier / 100f;
        if (factor <= 0f) return;

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target == null || target.IsDead) continue;

            target.TakeDamage(caster, factor);
        }
    }

    private static void ExecuteModifyStatPercent(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
    {
        Debug.LogWarning("SkillExecutor: ModifyStatPercent mới bridge tạm, chưa nối BuffData runtime.");
    }

    private static void ExecuteApplyStatus(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
    {
        Debug.LogWarning("SkillExecutor: ApplyStatus mới bridge tạm, chưa nối Buff/Status runtime.");
    }

    private static void ExecuteHealPercentMaxHp(PlayerHeroController caster, SkillEffectData effect)
    {
        if (caster == null) return;

        float healValue = caster.MaxHp * (effect.Value / 100f);
        if (healValue <= 0f) return;

        caster.Heal(healValue);
    }

    private static List<MonsterScrepController> ResolveTargets(PlayerHeroController caster, SkillTargetType targetType)
    {
        switch (targetType)
        {
            case SkillTargetType.CurrentTarget:
                return ResolveCurrentTarget(caster);

            case SkillTargetType.AllEnemies:
                return ResolveAllEnemies(caster);

            case SkillTargetType.RandomEnemy:
                return ResolveRandomEnemy(caster);

            case SkillTargetType.LowestHpEnemy:
                return ResolveLowestHpEnemy(caster);

            case SkillTargetType.HighestHpEnemy:
                return ResolveHighestHpEnemy(caster);

            case SkillTargetType.AreaAroundTarget:
                return ResolveAreaAroundTarget(caster);

            case SkillTargetType.Self:
            case SkillTargetType.AllAllies:
            case SkillTargetType.AreaAroundSelf:
                Debug.LogWarning($"SkillExecutor: TargetType {targetType} chưa implement cho hero-side.");
                return new List<MonsterScrepController>();

            default:
                return ResolveCurrentTarget(caster);
        }
    }

    private static List<MonsterScrepController> ResolveCurrentTarget(PlayerHeroController caster)
    {
        var result = new List<MonsterScrepController>();
        var target = caster.MonsterTarget;

        if (target != null && !target.IsDead)
            result.Add(target);

        return result;
    }

    private static List<MonsterScrepController> ResolveAllEnemies(PlayerHeroController caster)
    {
        var result = new List<MonsterScrepController>();
        var list = PvEBattleController.Instance?.MonsterList;

        if (list == null) return result;

        for (int i = 0; i < list.Count; i++)
        {
            var target = list[i];
            if (target == null || target.IsDead) continue;
            result.Add(target);
        }

        return result;
    }

    private static List<MonsterScrepController> ResolveRandomEnemy(PlayerHeroController caster)
    {
        var all = ResolveAllEnemies(caster);
        if (all.Count <= 1) return all;

        int idx = UnityEngine.Random.Range(0, all.Count);
        return new List<MonsterScrepController> { all[idx] };
    }

    private static List<MonsterScrepController> ResolveLowestHpEnemy(PlayerHeroController caster)
    {
        var all = ResolveAllEnemies(caster);
        if (all.Count == 0) return all;

        MonsterScrepController selected = all[0];
        for (int i = 1; i < all.Count; i++)
        {
            if (all[i].CurrentHp < selected.CurrentHp)
                selected = all[i];
        }

        return new List<MonsterScrepController> { selected };
    }

    private static List<MonsterScrepController> ResolveHighestHpEnemy(PlayerHeroController caster)
    {
        var all = ResolveAllEnemies(caster);
        if (all.Count == 0) return all;

        MonsterScrepController selected = all[0];
        for (int i = 1; i < all.Count; i++)
        {
            if (all[i].CurrentHp > selected.CurrentHp)
                selected = all[i];
        }

        return new List<MonsterScrepController> { selected };
    }

    private static List<MonsterScrepController> ResolveAreaAroundTarget(PlayerHeroController caster)
    {
        // Tạm thời bridge giống CurrentTarget.
        // Sau này nếu cần radius thật thì lấy thêm từ phase/effect data.
        return ResolveCurrentTarget(caster);
    }

    private static bool RollChance(float chancePercent)
    {
        if (chancePercent >= 100f) return true;
        if (chancePercent <= 0f) return false;

        return UnityEngine.Random.value * 100f <= chancePercent;
    }
}