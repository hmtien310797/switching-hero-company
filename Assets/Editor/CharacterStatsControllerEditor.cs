using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

[CustomEditor(typeof(StatsController))]
public class CharacterStatsControllerEditor : UnityEditor.Editor
{
    private bool showRuntimeDebug = true;
    private bool showHealth = true;
    private bool showStats = true;
    private bool showBuffs = true;
    private bool showStatus = true;
    private bool showDebugActions = true;

    private readonly Dictionary<StatType, bool> expandedStatModifiers = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var controller = (StatsController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Các thông tin runtime chỉ hiện khi đang Play Mode.", MessageType.Info);
            return;
        }

        if (controller.StatModule == null)
        {
            EditorGUILayout.HelpBox("StatModule chưa được init.", MessageType.Warning);
            return;
        }

        showRuntimeDebug = EditorGUILayout.Foldout(showRuntimeDebug, "Character Runtime Debug", true);
        if (!showRuntimeDebug)
            return;

        DrawHealthSection(controller);
        DrawStatsSection(controller);
        DrawBuffSection(controller);
        DrawStatusSection(controller);
        DrawDebugActions(controller);

        Repaint();
    }

    private void DrawHealthSection(StatsController controller)
    {
        showHealth = EditorGUILayout.Foldout(showHealth, "Health", true);
        if (!showHealth) return;

        using (new EditorGUI.IndentLevelScope())
        {
            float currentHp = controller.HealthModule != null ? controller.HealthModule.CurrentHP : 0f;
            float maxHp = controller.HealthModule != null ? controller.HealthModule.MaxHP : 0f;

            EditorGUILayout.LabelField("Current HP", currentHp.ToString("0.##"));
            EditorGUILayout.LabelField("Max HP", maxHp.ToString("0.##"));

            float ratio = maxHp > 0f ? currentHp / maxHp : 0f;
            Rect rect = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.ProgressBar(rect, ratio, $"{currentHp:0.##} / {maxHp:0.##}");
            EditorGUILayout.Space(4);
        }
    }

    private void DrawStatsSection(StatsController controller)
    {
        showStats = EditorGUILayout.Foldout(showStats, "Base Stat / Current Stat / Modifiers", true);
        if (!showStats) return;

        var allStats = controller.StatModule.GetAllStats();
        if (allStats == null || allStats.Count == 0)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("No stat data.");
            }
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stat", EditorStyles.miniBoldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Base", EditorStyles.miniBoldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField("Current", EditorStyles.miniBoldLabel, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            foreach (var pair in allStats)
            {
                var statType = pair.Key;
                var runtimeStat = pair.Value;
                if (runtimeStat == null) continue;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(statType.ToString(), GUILayout.Width(150));
                EditorGUILayout.LabelField(runtimeStat.BaseValue.ToString("0.##"), GUILayout.Width(90));
                EditorGUILayout.LabelField(runtimeStat.FinalValue.ToString("0.##"), GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();

                var modifiers = runtimeStat.GetModifiers();
                if (modifiers != null && modifiers.Count > 0)
                {
                    if (!expandedStatModifiers.ContainsKey(statType))
                        expandedStatModifiers[statType] = false;

                    expandedStatModifiers[statType] = EditorGUILayout.Foldout(
                        expandedStatModifiers[statType],
                        $"Modifiers ({modifiers.Count})",
                        true
                    );

                    if (expandedStatModifiers[statType])
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            for (int i = 0; i < modifiers.Count; i++)
                            {
                                var mod = modifiers[i];
                                if (mod == null) continue;

                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(mod.SourceId, GUILayout.Width(150));
                                EditorGUILayout.LabelField(mod.Operation.ToString(), GUILayout.Width(70));
                                EditorGUILayout.LabelField(FormatModifierValue(mod), GUILayout.Width(80));
                                EditorGUILayout.EndHorizontal();

                                EditorGUILayout.LabelField($"Target: {mod.StatType}", EditorStyles.miniLabel);
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No modifiers", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawBuffSection(StatsController controller)
    {
        showBuffs = EditorGUILayout.Foldout(showBuffs, "Active Buffs", true);
        if (!showBuffs) return;

        using (new EditorGUI.IndentLevelScope())
        {
            var buffs = controller.BuffModule != null ? controller.BuffModule.ActiveBuffs : null;

            if (buffs == null || buffs.Count == 0)
            {
                EditorGUILayout.LabelField("No active buffs.");
                return;
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                if (buff == null || buff.Data == null) continue;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField(buff.Data.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Id", buff.Data.Id);
                EditorGUILayout.LabelField("Kind", buff.Data.Kind.ToString());
                EditorGUILayout.LabelField("Stack", buff.StackCount.ToString());
                EditorGUILayout.LabelField("Remaining Time", $"{buff.RemainingTime:0.00}s");

                if (buff.Data.StatusEffects != StatusEffectType.None)
                {
                    EditorGUILayout.LabelField("Status Effect", buff.Data.StatusEffects.ToString());
                }

                if (buff.Data.PeriodicEffectType != PeriodicEffectType.None)
                {
                    EditorGUILayout.LabelField("Periodic Type", buff.Data.PeriodicEffectType.ToString());
                    EditorGUILayout.LabelField("Tick Interval", $"{buff.Data.TickInterval:0.##}s");
                    EditorGUILayout.LabelField("Tick Value", buff.Data.PeriodicValue.ToString("0.##"));
                    EditorGUILayout.LabelField("Damage Type", buff.Data.PeriodicDamageType.ToString());
                }

                if (buff.Data.Modifiers != null && buff.Data.Modifiers.Count > 0)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Buff Modifiers", EditorStyles.miniBoldLabel);

                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int j = 0; j < buff.Data.Modifiers.Count; j++)
                        {
                            var mod = buff.Data.Modifiers[j];
                            if (mod == null) continue;

                            EditorGUILayout.LabelField(
                                $"{mod.StatType} | {mod.Operation} | {FormatModifierValue(mod)}"
                            );
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }
    }

    private void DrawStatusSection(StatsController controller)
    {
        showStatus = EditorGUILayout.Foldout(showStatus, "Status Effects", true);
        if (!showStatus) return;

        using (new EditorGUI.IndentLevelScope())
        {
            var currentStatus = controller.StatusEffectModule != null
                ? controller.StatusEffectModule.CurrentStatus
                : StatusEffectType.None;

            EditorGUILayout.LabelField("Current Status", currentStatus.ToString());

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Stun", (currentStatus & StatusEffectType.Stun) != 0);
                EditorGUILayout.Toggle("Silence", (currentStatus & StatusEffectType.Silence) != 0);
                EditorGUILayout.Toggle("Freeze", (currentStatus & StatusEffectType.Freeze) != 0);
            }
        }
    }

    private void DrawDebugActions(StatsController controller)
    {
        showDebugActions = EditorGUILayout.Foldout(showDebugActions, "Debug Actions", true);
        if (!showDebugActions) return;

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Apply Poison"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreatePoison());
            }

            if (GUILayout.Button("Apply Burn"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreateBurn());
            }

            if (GUILayout.Button("Apply Regen"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreateRegen());
            }

            if (GUILayout.Button("Apply Stun"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreateStun());
            }

            if (GUILayout.Button("Apply Silence"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreateSilence());
            }

            if (GUILayout.Button("Apply Freeze"))
            {
                controller.BuffModule.ApplyBuff(BuffFactory.CreateFreeze());
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Damage 100"))
            {
                controller.HealthModule.ApplyDamage(100f);
            }

            if (GUILayout.Button("Heal 100"))
            {
                controller.HealthModule.ApplyHeal(100f);
            }

            if (GUILayout.Button("Add DEF +30 Manual"))
            {
                controller.StatModule.AddModifier(new StatModifier(
                    StatType.DEF,
                    ModifierOp.Add,
                    30f,
                    "debug_manual_def_30"
                ));
            }

            if (GUILayout.Button("Remove DEF +30 Manual"))
            {
                controller.StatModule.RemoveModifiersBySource("debug_manual_def_30");
            }

            EditorGUILayout.EndVertical();
        }
    }

    private string FormatModifierValue(StatModifier mod)
    {
        if (mod.Operation == ModifierOp.Multiply)
        {
            float percent = mod.Value * 100f;
            return $"{percent:+0.##;-0.##;0}%";
        }

        return mod.Value.ToString("+0.##;-0.##;0");
    }
}