using System.Collections.Generic;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillDataSO))]
public class SkillDataSOEditor : UnityEditor.Editor
{
    private int quickApplyLevel = 1;
    private float quickHitDamage = 200f;
    private float quickFinalHitDamage = 60f;

    private float generateStartHitDamage = 200f;
    private float generateStartFinalHitDamage = 60f;
    private float generateHitDamagePerLevel = 0f;
    private float generateFinalHitDamagePerLevel = 0f;
    private bool clearAndRebuild = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        DrawGeneratorTools();
        EditorGUILayout.Space(12);
        DrawQuickApplyTools();
        EditorGUILayout.Space(12);
        DrawPreviewTools();
    }

    private void DrawGeneratorTools()
    {
        var skillData = (SkillDataSO)target;
        if (skillData == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Skill Level Generator", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Generate toàn bộ Levels dựa trên MaxLevel. Mỗi level sẽ có 2 phase mặc định: 'hit' và 'finalhit'.",
            MessageType.Info);

        generateStartHitDamage = EditorGUILayout.FloatField("Start Hit Damage", generateStartHitDamage);
        generateStartFinalHitDamage = EditorGUILayout.FloatField("Start FinalHit Damage", generateStartFinalHitDamage);
        generateHitDamagePerLevel = EditorGUILayout.FloatField("Hit Damage Per Level", generateHitDamagePerLevel);
        generateFinalHitDamagePerLevel = EditorGUILayout.FloatField("FinalHit Damage Per Level", generateFinalHitDamagePerLevel);
        clearAndRebuild = EditorGUILayout.Toggle("Clear And Rebuild", clearAndRebuild);

        GUI.enabled = skillData.MaxLevel > 0;
        if (GUILayout.Button("Generate Levels"))
        {
            GenerateLevels(skillData);
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickApplyTools()
    {
        var skillData = (SkillDataSO)target;
        if (skillData == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Apply One Level", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Set nhanh damage cho 1 level cụ thể. Tool này sẽ tự đảm bảo level đó có phase 'hit' và 'finalhit'.",
            MessageType.None);

        quickApplyLevel = EditorGUILayout.IntField("Level", quickApplyLevel);
        quickHitDamage = EditorGUILayout.FloatField("Hit Damage", quickHitDamage);
        quickFinalHitDamage = EditorGUILayout.FloatField("FinalHit Damage", quickFinalHitDamage);

        GUI.enabled = quickApplyLevel > 0;
        if (GUILayout.Button("Apply To Level"))
        {
            ApplyToSingleLevel(skillData, quickApplyLevel, quickHitDamage, quickFinalHitDamage);
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewTools()
    {
        var skillData = (SkillDataSO)target;
        if (skillData == null) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Description Preview", EditorStyles.boldLabel);

        int previewLevel = Mathf.Clamp(
            EditorGUILayout.IntField("Preview Level", quickApplyLevel),
            1,
            Mathf.Max(1, skillData.MaxLevel));

        string preview = skillData.BuildDescription(previewLevel);
        EditorGUILayout.TextArea(string.IsNullOrEmpty(preview) ? "(Empty Description)" : preview, GUILayout.MinHeight(50));

        EditorGUILayout.EndVertical();
    }

    private void GenerateLevels(SkillDataSO skillData)
    {
        Undo.RecordObject(skillData, "Generate Skill Levels");

        if (skillData.Levels == null)
            skillData.Levels = new List<SkillLevelData>();

        if (clearAndRebuild)
            skillData.Levels.Clear();

        for (int level = 1; level <= Mathf.Max(1, skillData.MaxLevel); level++)
        {
            float hitDamage = generateStartHitDamage + generateHitDamagePerLevel * (level - 1);
            float finalHitDamage = generateStartFinalHitDamage + generateFinalHitDamagePerLevel * (level - 1);

            SkillLevelData levelData = GetOrCreateLevel(skillData, level);
            EnsureDefaultDamagePhases(levelData, hitDamage, finalHitDamage);
        }

        MarkDirty(skillData);
    }

    private void ApplyToSingleLevel(SkillDataSO skillData, int level, float hitDamage, float finalHitDamage)
    {
        Undo.RecordObject(skillData, "Apply Skill Level Data");

        if (skillData.MaxLevel < level)
            skillData.MaxLevel = level;

        if (skillData.Levels == null)
            skillData.Levels = new List<SkillLevelData>();

        SkillLevelData levelData = GetOrCreateLevel(skillData, level);
        EnsureDefaultDamagePhases(levelData, hitDamage, finalHitDamage);

        MarkDirty(skillData);
    }

    private SkillLevelData GetOrCreateLevel(SkillDataSO skillData, int level)
    {
        for (int i = 0; i < skillData.Levels.Count; i++)
        {
            if (skillData.Levels[i] != null && skillData.Levels[i].Level == level)
                return skillData.Levels[i];
        }

        SkillLevelData newLevel = new SkillLevelData
        {
            Level = level,
            Phases = new List<SkillPhaseData>(),
            DescriptionParams = new List<SkillDescriptionParam>()
        };

        skillData.Levels.Add(newLevel);
        skillData.Levels.Sort((a, b) => a.Level.CompareTo(b.Level));
        return newLevel;
    }

    private void EnsureDefaultDamagePhases(SkillLevelData levelData, float hitDamage, float finalHitDamage)
    {
        if (levelData.Phases == null)
            levelData.Phases = new List<SkillPhaseData>();

        SkillPhaseData hitPhase = GetOrCreatePhase(levelData, "hit", 1);
        SkillPhaseData finalHitPhase = GetOrCreatePhase(levelData, "finalhit", 2);

        SetupDamageOnlyPhase(hitPhase, hitDamage);
        SetupDamageOnlyPhase(finalHitPhase, finalHitDamage);

        RefreshDescriptionParams(levelData, hitDamage, finalHitDamage);
    }

    private SkillPhaseData GetOrCreatePhase(SkillLevelData levelData, string eventName, int phaseId)
    {
        for (int i = 0; i < levelData.Phases.Count; i++)
        {
            if (levelData.Phases[i] != null && levelData.Phases[i].SpineEventName == eventName)
                return levelData.Phases[i];
        }

        SkillPhaseData newPhase = new SkillPhaseData
        {
            PhaseId = phaseId,
            SpineEventName = eventName,
            TargetTypeOverride = SkillTargetType.CurrentTarget,
            Effects = new List<SkillEffectData>()
        };

        levelData.Phases.Add(newPhase);
        return newPhase;
    }

    private void SetupDamageOnlyPhase(SkillPhaseData phase, float damageMultiplier)
    {
        phase.PhaseId = phase.SpineEventName == "hit" ? 1 : 2;
        phase.TargetTypeOverride = SkillTargetType.CurrentTarget;

        if (phase.Effects == null)
            phase.Effects = new List<SkillEffectData>();

        SkillEffectData damageEffect;
        if (phase.Effects.Count > 0 && phase.Effects[0] != null)
        {
            damageEffect = phase.Effects[0];
        }
        else
        {
            damageEffect = new SkillEffectData();
            if (phase.Effects.Count == 0)
                phase.Effects.Add(damageEffect);
            else
                phase.Effects[0] = damageEffect;
        }

        damageEffect.EffectType = SkillEffectType.Damage;
        damageEffect.DamageMultiplier = damageMultiplier;
        damageEffect.ChancePercent = 100f;
        damageEffect.ScalingStat = SkillScalingStat.Attack;
        damageEffect.TargetTypeOverride = SkillTargetType.CurrentTarget;

        damageEffect.Value = 0f;
        damageEffect.Duration = 0f;
        damageEffect.StackCount = 1;
        damageEffect.StatModifierPercent = 0f;
    }

    private void RefreshDescriptionParams(SkillLevelData levelData, float hitDamage, float finalHitDamage)
    {
        if (levelData.DescriptionParams == null)
            levelData.DescriptionParams = new List<SkillDescriptionParam>();
        else
            levelData.DescriptionParams.Clear();

        levelData.DescriptionParams.Add(new SkillDescriptionParam
        {
            Key = "damage1",
            Value = hitDamage,
            IsPercent = false,
            DecimalPlaces = 0
        });

        levelData.DescriptionParams.Add(new SkillDescriptionParam
        {
            Key = "damage2",
            Value = finalHitDamage,
            IsPercent = false,
            DecimalPlaces = 0
        });
    }

    private void MarkDirty(SkillDataSO skillData)
    {
        EditorUtility.SetDirty(skillData);
        AssetDatabase.SaveAssets();
    }
}