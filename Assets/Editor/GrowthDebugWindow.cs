#if UNITY_EDITOR
using System.Collections.Generic;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class GrowthDebugWindow : EditorWindow
    {
        private GrowthManager manager;
        private GrowthSystemService service;

        private Vector2 scroll;
        private List<StatType> statsCache = new();
        private Dictionary<StatType, bool> foldoutStates = new();

        [MenuItem("Tools/Growth Debug Window")]
        public static void Open()
        {
            GetWindow<GrowthDebugWindow>("Growth Debug");
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            manager = GrowthManager.Instance;
            service = manager != null ? manager.Service : null;

            statsCache.Clear();

            if (service != null)
            {
                statsCache = service.GetAllUnlockedStats();

                foreach (var stat in statsCache)
                {
                    if (!foldoutStates.ContainsKey(stat))
                        foldoutStates.Add(stat, true);
                }
            }

            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (manager == null || service == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Không tìm thấy GrowthManager hoặc Service chưa sẵn sàng.", MessageType.Warning);

                if (GUILayout.Button("Retry", GUILayout.Height(28)))
                    RefreshData();

                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawOverview();
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
                    if (EditorUtility.DisplayDialog("Clear Growth Data", "Xóa toàn bộ dữ liệu growth?", "Xóa", "Hủy"))
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
            DrawInfoRow("Gold", manager.PlayerGold.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawTotalStats()
        {
            EditorGUILayout.BeginVertical("box");

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            EditorGUILayout.LabelField("TOTAL STATS", titleStyle);
            EditorGUILayout.Space(4);

            if (statsCache.Count == 0)
            {
                EditorGUILayout.LabelField("Chưa có stat nào được unlock.");
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var stat in statsCache)
            {
                float total = service.GetTotalGrowthValue(stat);
                DrawInfoRow(stat.ToString(), FormatPercent(total));
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

            foreach (var stat in statsCache)
            {
                DrawStatCard(stat);
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
                $"{stat}   |   Total: {FormatPercent(total)}",
                true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold }
            );

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{current} / {max}", GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            float progress = max > 0 ? (float)current / max : 0f;
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, progress, $"Stack Progress: {current} / {max}");
            EditorGUILayout.Space(4);

            if (foldoutStates[stat])
            {
                DrawTierBreakdown(stat);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTierBreakdown(StatType stat)
        {
            int currentStack = service.GetCurrentStack(stat);

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

                float perLevelValuePercent = growth.ValuePerLevel;
                float tierTotalDecimal = usedStack * (perLevelValuePercent / 100f);

                Color oldColor = GUI.backgroundColor;

                bool isFullTier = usedStack >= tierStackCapacity;
                GUI.backgroundColor = isFullTier
                    ? new Color(0.85f, 1f, 0.85f, 1f)
                    : new Color(1f, 0.97f, 0.85f, 1f);

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = oldColor;

                EditorGUILayout.LabelField($"Tier {tier}", new GUIStyle(EditorStyles.boldLabel));
                DrawInfoRow("Value Per Level", $"{perLevelValuePercent:0.###}%");
                DrawInfoRow("Used Stack", $"{usedStack} / {tierStackCapacity}");
                DrawInfoRow("Tier Total", FormatPercent(tierTotalDecimal));

                float tierProgress = tierStackCapacity > 0 ? (float)usedStack / tierStackCapacity : 0f;
                Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(rect, tierProgress, $"Tier Progress: {usedStack} / {tierStackCapacity}");

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }

        private string FormatPercent(float value)
        {
            return $"{value * 100f:0.##}%";
        }
    }
}
#endif