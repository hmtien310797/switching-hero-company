#if UNITY_EDITOR
using System.Collections.Generic;
using Immortal_Switch.Scripts.PowerUpSystem;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthDebugWindow : EditorWindow
    {
        private GrowthManager manager;
        private GrowthSystemService service;

        private Vector2 scroll;
        private readonly List<StatType> statsCache = new();
        private readonly Dictionary<StatType, bool> foldoutStates = new();
        private readonly List<PowerUpModifierData> exportedPowerUps = new();

        [MenuItem("Tools/Growth Debug Window")]
        public static void Open()
        {
            GetWindow<GrowthDebugWindow>("Growth Debug");
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            manager = PowerUpManager.Instance.GrowthManager;
            service = manager != null ? manager.Service : null;

            statsCache.Clear();
            exportedPowerUps.Clear();

            if (service != null)
            {
                var stats = service.GetAllUnlockedStats();
                for (int i = 0; i < stats.Count; i++)
                {
                    var stat = stats[i];
                    statsCache.Add(stat);

                    if (!foldoutStates.ContainsKey(stat))
                        foldoutStates.Add(stat, true);
                }

                service.CollectPowerUps(exportedPowerUps);
            }

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (manager == null || service == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Không tìm thấy GrowthManager hoặc GrowthSystemService chưa sẵn sàng.", MessageType.Warning);

                if (GUILayout.Button("Retry", GUILayout.Height(28)))
                    RefreshData();

                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawOverview();
            EditorGUILayout.Space(8);

            DrawExportPreview();
            EditorGUILayout.Space(8);

            DrawTotalStats();
            EditorGUILayout.Space(8);

            DrawDetailStats();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshData();
            }

            if (manager != null)
            {
                if (GUILayout.Button("Add 10k Gold", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    manager.AddGold(10000);
                    RefreshData();
                }

                if (GUILayout.Button("Unlock Next Tier", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    manager.UnlockTier(manager.SaveData.CurrentUnlockedTier + 1);
                    RefreshData();
                }

                if (GUILayout.Button("Clear Data", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Clear Growth Data",
                        "Xóa toàn bộ dữ liệu Growth?",
                        "Xóa",
                        "Hủy"
                    );

                    if (confirm)
                    {
                        manager.ClearData();
                        RefreshData();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical("box");

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            EditorGUILayout.LabelField("OVERVIEW", titleStyle);
            EditorGUILayout.Space(4);

            DrawInfoRow("Current Tier", manager.SaveData.CurrentUnlockedTier.ToString());
            DrawInfoRow("Max Tier", service.MaxTier.ToString());
            DrawInfoRow("Gold", manager.PlayerGold.ToString("N0"));
            DrawInfoRow("Unlocked Stats", statsCache.Count.ToString());
            DrawInfoRow("Exported PowerUps", exportedPowerUps.Count.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawExportPreview()
        {
            EditorGUILayout.BeginVertical("box");

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            EditorGUILayout.LabelField("POWER UP EXPORT PREVIEW", titleStyle);
            EditorGUILayout.Space(4);

            if (exportedPowerUps.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có power up nào được export từ Growth.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            for (int i = 0; i < exportedPowerUps.Count; i++)
            {
                var item = exportedPowerUps[i];

                EditorGUILayout.BeginVertical("helpbox");
                DrawInfoRow("Source", item.SourceId);
                DrawInfoRow("Stat", item.TargetStat.ToString());
                DrawInfoRow("Kind", item.ValueKind.ToString());
                DrawInfoRow("Value", service.FormatForDisplay(item.TargetStat, item.Value));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTotalStats()
        {
            EditorGUILayout.BeginVertical("box");

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            EditorGUILayout.LabelField("TOTAL GROWTH VALUES", titleStyle);
            EditorGUILayout.Space(4);

            if (statsCache.Count == 0)
            {
                EditorGUILayout.LabelField("Chưa có stat nào được unlock.");
                EditorGUILayout.EndVertical();
                return;
            }

            for (int i = 0; i < statsCache.Count; i++)
            {
                var stat = statsCache[i];
                float total = service.GetTotalGrowthValue(stat);
                DrawInfoRow(stat.ToString(), service.FormatForDisplay(stat, total));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDetailStats()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            EditorGUILayout.LabelField("DETAIL STATS", titleStyle);
            EditorGUILayout.Space(4);

            if (statsCache.Count == 0)
            {
                EditorGUILayout.HelpBox("Không có stat nào để hiển thị.", MessageType.Info);
                return;
            }

            for (int i = 0; i < statsCache.Count; i++)
            {
                DrawStatCard(statsCache[i]);
                EditorGUILayout.Space(6);
            }
        }

        private void DrawStatCard(StatType stat)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.90f, 0.95f, 1f, 1f);

            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = oldColor;

            float total = service.GetTotalGrowthValue(stat);
            int current = service.GetCurrentStack(stat);
            int max = service.GetMaxAvailableStack(stat);

            EditorGUILayout.BeginHorizontal();

            foldoutStates[stat] = EditorGUILayout.Foldout(
                foldoutStates[stat],
                $"{stat}   |   Total: {service.FormatForDisplay(stat, total)}",
                true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }
            );

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{current} / {max}", GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();

            float progress = max > 0 ? (float)current / max : 0f;
            Rect totalRect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(totalRect, progress, $"Stack Progress: {current} / {max}");
            EditorGUILayout.Space(4);

            if (foldoutStates[stat])
            {
                DrawStatSummary(stat, total, current, max);
                EditorGUILayout.Space(4);
                DrawTierBreakdown(stat);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatSummary(StatType stat, float total, int current, int max)
        {
            EditorGUILayout.BeginVertical("helpbox");

            DrawInfoRow("Value Type", service.GetValueType(stat).ToString());
            DrawInfoRow("Total Value", service.FormatForDisplay(stat, total));
            DrawInfoRow("Current Stack", $"{current} / {max}");

            var preview = BuildPreviewModifier(stat, total);
            DrawInfoRow("PowerUp Kind", preview.ValueKind.ToString());

            EditorGUILayout.EndVertical();
        }

        private PowerUpModifierData BuildPreviewModifier(StatType stat, float totalValue)
        {
            bool isPercentValue = service.IsPercentValue(stat);

            switch (stat)
            {
                case StatType.Atk:
                case StatType.MaxHp:
                case StatType.Def:
                    return new PowerUpModifierData(
                        service.SourceId,
                        stat,
                        isPercentValue ? PowerUpValueKind.PercentOfBase : PowerUpValueKind.FlatAdd,
                        totalValue
                    );

                default:
                    return new PowerUpModifierData(
                        service.SourceId,
                        stat,
                        PowerUpValueKind.FlatAdd,
                        totalValue
                    );
            }
        }

        private void DrawTierBreakdown(StatType stat)
        {
            int currentStack = service.GetCurrentStack(stat);
            bool hasAnyTierShown = false;

            for (int tier = 1; tier <= service.MaxTier; tier++)
            {
                if (!service.TryGetStatGrowthAtTier(tier, stat, out var growth))
                    continue;

                var tierData = service.GetTierData(tier);
                if (tierData == null)
                    continue;

                int prevMax = 0;
                var prevTier = service.GetTierData(tier - 1);
                if (prevTier != null)
                    prevMax = prevTier.MaxStack;

                int tierStackCapacity = tierData.MaxStack - prevMax;
                int usedStack = Mathf.Clamp(currentStack - prevMax, 0, tierStackCapacity);

                if (usedStack <= 0)
                    continue;

                hasAnyTierShown = true;

                float perLevelRaw = growth.ValuePerLevel;
                float perLevelRuntime = service.ConvertRawValueToRuntime(stat, perLevelRaw);
                float tierTotalRuntime = usedStack * perLevelRuntime;

                Color oldColor = GUI.backgroundColor;
                bool isFullTier = usedStack >= tierStackCapacity;

                GUI.backgroundColor = isFullTier
                    ? new Color(0.85f, 1f, 0.85f, 1f)
                    : new Color(1f, 0.97f, 0.85f, 1f);

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = oldColor;

                EditorGUILayout.LabelField($"Tier {tier}", new GUIStyle(EditorStyles.boldLabel));

                DrawInfoRow("Value Type", growth.ValueType.ToString());
                DrawInfoRow("Value Per Level", service.FormatRawPerLevelForDisplay(stat, perLevelRaw));
                DrawInfoRow("Used Stack", $"{usedStack} / {tierStackCapacity}");
                DrawInfoRow("Tier Total", service.FormatForDisplay(stat, tierTotalRuntime));

                float tierProgress = tierStackCapacity > 0 ? (float)usedStack / tierStackCapacity : 0f;
                Rect tierRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(tierRect, tierProgress, $"Tier Progress: {usedStack} / {tierStackCapacity}");

                EditorGUILayout.EndVertical();
            }

            if (!hasAnyTierShown)
            {
                EditorGUILayout.HelpBox("Stat này chưa có tier nào đóng góp vào value hiện tại.", MessageType.Info);
            }
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif