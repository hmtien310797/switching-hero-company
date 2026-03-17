using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;

[CustomEditor(typeof(StatsController))]
public class CharacterStatsControllerEditor : UnityEditor.Editor
{
    private bool showHealth = true;
    private bool showStats = true;
    private bool showBuffs = true;
    private bool showDots = true;
    private bool showStatus = true;
    private bool showActions = true;

    private readonly Dictionary<StatType, bool> expandedStatModifiers = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var controller = (StatsController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Runtime debug chỉ hiển thị khi đang Play Mode.", MessageType.Info);
            return;
        }

        if (controller.StatModule == null)
        {
            EditorGUILayout.HelpBox("StatModule chưa được init.", MessageType.Warning);
            return;
        }

        DrawHealthSection(controller);
        DrawStatsSection(controller);
        DrawBuffSection(controller);
        DrawDotSection(controller);
        DrawStatusSection(controller);
        DrawActionSection(controller);

        Repaint();
    }

    private void DrawHealthSection(StatsController controller)
    {
        showHealth = EditorGUILayout.Foldout(showHealth, "Health", true);
        if (!showHealth) return;

        using (new EditorGUI.IndentLevelScope())
        {
            float current = controller.HealthModule != null ? controller.HealthModule.CurrentHP : 0f;
            float max = controller.HealthModule != null ? controller.HealthModule.MaxHP : 0f;

            EditorGUILayout.LabelField("Current HP", current.ToString("0.##"));
            EditorGUILayout.LabelField("Max HP", max.ToString("0.##"));

            float ratio = max > 0f ? current / max : 0f;
            Rect rect = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.ProgressBar(rect, ratio, $"{current:0.##} / {max:0.##}");
            EditorGUILayout.Space(4);
        }
    }

    private void DrawStatsSection(StatsController controller)
    {
        showStats = EditorGUILayout.Foldout(showStats, "Base Stats / Current Stats", true);
        if (!showStats) return;

        var allStats = controller.StatModule.GetAllStats();
        if (allStats == null || allStats.Count == 0)
        {
            using (new EditorGUI.IndentLevelScope())
                EditorGUILayout.LabelField("No stats.");
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
                                EditorGUILayout.LabelField(string.IsNullOrEmpty(mod.SourceId) ? "NoSource" : mod.SourceId, GUILayout.Width(150));
                                EditorGUILayout.LabelField(mod.Operation.ToString(), GUILayout.Width(70));
                                EditorGUILayout.LabelField(FormatModifierValue(mod), GUILayout.Width(80));
                                EditorGUILayout.EndHorizontal();
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
                EditorGUILayout.LabelField("Remaining", $"{buff.RemainingTime:0.00}s");

                if (buff.Data.StatusEffects != StatusEffectType.None)
                    EditorGUILayout.LabelField("Status", buff.Data.StatusEffects.ToString());

                if (buff.Data.PeriodicEffectType != PeriodicEffectType.None)
                {
                    EditorGUILayout.LabelField("Periodic", buff.Data.PeriodicEffectType.ToString());
                    EditorGUILayout.LabelField("Tick Interval", $"{buff.Data.TickInterval:0.##}s");
                    EditorGUILayout.LabelField("Periodic Value", buff.Data.PeriodicValue.ToString("0.##"));
                    EditorGUILayout.LabelField("Damage Type", buff.Data.PeriodicDamageType.ToString());
                }

                if (buff.Data.Modifiers != null && buff.Data.Modifiers.Count > 0)
                {
                    EditorGUILayout.LabelField("Modifiers", EditorStyles.miniBoldLabel);

                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int j = 0; j < buff.Data.Modifiers.Count; j++)
                        {
                            var mod = buff.Data.Modifiers[j];
                            if (mod == null) continue;
                            EditorGUILayout.LabelField($"{mod.StatType} | {mod.Operation} | {FormatModifierValue(mod)}");
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }
    }

    private void DrawDotSection(StatsController controller)
    {
        showDots = EditorGUILayout.Foldout(showDots, "Active DOT", true);
        if (!showDots) return;

        using (new EditorGUI.IndentLevelScope())
        {
            var dots = controller.DotModule != null ? controller.DotModule.ActiveDots : null;

            if (dots == null || dots.Count == 0)
            {
                EditorGUILayout.LabelField("No active DOT.");
                return;
            }

            for (int i = 0; i < dots.Count; i++)
            {
                var dot = dots[i];
                if (dot == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(dot.EffectId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Damage Type", dot.DamageType.ToString());
                EditorGUILayout.LabelField("Stack Rule", dot.StackRule.ToString());
                EditorGUILayout.LabelField("Tick Damage", dot.TickDamage.ToString("0.##"));
                EditorGUILayout.LabelField("Tick Interval", $"{dot.TickInterval:0.##}s");
                EditorGUILayout.LabelField("Remaining", $"{dot.RemainingDuration:0.00}s");

                string sourceName = dot.Source != null ? dot.Source.Stats.gameObject.name : "None";
                string targetName = dot.Target != null ? dot.Target.Stats.gameObject.name : "None";

                EditorGUILayout.LabelField("Source", sourceName);
                EditorGUILayout.LabelField("Target", targetName);
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
            var current = controller.StatusEffectModule != null
                ? controller.StatusEffectModule.CurrentStatus
                : StatusEffectType.None;

            EditorGUILayout.LabelField("Current Status", current.ToString());

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Stun", (current & StatusEffectType.Stun) != 0);
                EditorGUILayout.Toggle("Silence", (current & StatusEffectType.Silence) != 0);
                EditorGUILayout.Toggle("Freeze", (current & StatusEffectType.Freeze) != 0);
            }
        }
    }

    private void DrawActionSection(StatsController controller)
    {
        showActions = EditorGUILayout.Foldout(showActions, "Debug Actions", true);
        if (!showActions) return;

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Damage 100"))
                controller.HealthModule.TakeDamage(100f, DamageType.Normal);

            if (GUILayout.Button("Heal 100"))
                controller.HealthModule.ApplyHeal(100f);

            if (GUILayout.Button("Add DEF +30"))
            {
                controller.StatModule.AddModifier(
                    new StatModifier(StatType.DEF, ModifierOp.Add, 30f, "debug_def_30")
                );
            }

            if (GUILayout.Button("Remove DEF +30"))
            {
                controller.StatModule.RemoveModifiersBySource("debug_def_30");
            }

            if (GUILayout.Button("Apply Burn DOT Snapshot"))
            {
                // ví dụ snapshot hệ số 0.4
                var attacker = controller.gameObject.GetComponent<ICombatUnit>();
                DamageResult result = DamageCalculator.CalculateDamage(attacker, attacker, 0.4f);

                controller.DotModule.ApplyDotSnapshot(
                    effectId: "burn",
                    source: attacker,
                    target: attacker,
                    tickDamage: result.Damage,
                    tickInterval: 1f,
                    duration: 4f,
                    damageType: DamageType.Burn,
                    stackRule: DotStackRule.StackIndependent
                );
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