#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpDebugWindow : EditorWindow
    {
        private PowerUpManager manager;
        private PowerUpSystemService service;

        private readonly List<StatsController> playerList = new();
        private readonly Dictionary<string, bool> statFoldouts = new();
        private readonly Dictionary<string, bool> sourceFoldouts = new();

        private Vector2 scroll;
        private int selectedPlayerIndex;
        private bool showAllPlayers;

        private bool showLegend = true;
        private bool showRawModifiers = true;
        private bool showSnapshot = true;
        private bool showPlayerStats = true;
        private bool showStatModuleModifiers = true;

        [MenuItem("Tools/PowerUp Debug Window")]
        public static void Open()
        {
            GetWindow<PowerUpDebugWindow>("PowerUp Debug");
        }

        private void OnEnable()
        {
            RefreshData();
            EditorApplication.playModeStateChanged += _ => RefreshData();
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void RefreshData()
        {
            manager = PowerUpManager.Instance;
            service = manager != null ? manager.Service : null;

            RefreshPlayers();

            if (service != null && service.RawModifiersBySource != null)
            {
                foreach (var pair in service.RawModifiersBySource)
                {
                    if (!sourceFoldouts.ContainsKey(pair.Key))
                        sourceFoldouts.Add(pair.Key, true);
                }
            }
        }

        // =========================
        // PLAYER DETECTION
        // =========================
        private void RefreshPlayers()
        {
            playerList.Clear();

            var all = FindObjectsByType<PlayerHeroController>(FindObjectsSortMode.None);
            if (all == null) return;

            // fallback cuối
            if (playerList.Count == 0)
            {
                foreach (var item in all)
                {
                    if (item != null)
                        playerList.Add(item.Stats);
                }
            }

            if (selectedPlayerIndex >= playerList.Count)
                selectedPlayerIndex = Mathf.Max(0, playerList.Count - 1);
        }

        private StatsController GetSelectedPlayer()
        {
            if (playerList.Count == 0) return null;
            return playerList[Mathf.Clamp(selectedPlayerIndex, 0, playerList.Count - 1)];
        }

        private string[] GetPlayerNames()
        {
            var names = new string[playerList.Count];
            for (int i = 0; i < playerList.Count; i++)
            {
                names[i] = $"{i + 1}. {playerList[i].name}";
            }
            return names;
        }

        // =========================
        // GUI
        // =========================
        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode only", MessageType.Info);
                return;
            }

            if (manager == null || service == null)
            {
                EditorGUILayout.HelpBox("PowerUpManager chưa sẵn sàng", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawOverview();
            DrawPlayerSelector();
            DrawSnapshot();
            DrawPlayers();
            DrawModifiers();

            EditorGUILayout.EndScrollView();
        }

        // =========================
        // OVERVIEW
        // =========================
        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("OVERVIEW", EditorStyles.boldLabel);

            DrawRow("Players", playerList.Count.ToString());
            DrawRow("Raw Modifiers", service.RawModifiers.Count.ToString());
            DrawRow("Sources", service.RawModifiersBySource.Count.ToString());

            EditorGUILayout.EndVertical();
        }

        // =========================
        // PLAYER SELECT
        // =========================
        private void DrawPlayerSelector()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("PLAYER SELECTOR", EditorStyles.boldLabel);

            showAllPlayers = EditorGUILayout.Toggle("Show All", showAllPlayers);

            using (new EditorGUI.DisabledScope(showAllPlayers))
            {
                selectedPlayerIndex = EditorGUILayout.Popup(
                    "Player",
                    selectedPlayerIndex,
                    GetPlayerNames()
                );
            }

            EditorGUILayout.EndVertical();
        }

        // =========================
        // SNAPSHOT
        // =========================
        private void DrawSnapshot()
        {
            showSnapshot = EditorGUILayout.Foldout(showSnapshot, "SNAPSHOT");
            if (!showSnapshot) return;

            var snapshot = service.CurrentSnapshot;

            EditorGUILayout.BeginVertical("box");

            foreach (var pair in snapshot.FlatAdds)
            {
                DrawRow(pair.Key.ToString(), pair.Value.ToString());
            }

            foreach (var pair in snapshot.BasePercents)
            {
                DrawRow(pair.Key.ToString(), $"{pair.Value * 100f:0.##}%");
            }

            EditorGUILayout.EndVertical();
        }

        // =========================
        // PLAYER STATS
        // =========================
        private void DrawPlayers()
        {
            showPlayerStats = EditorGUILayout.Foldout(showPlayerStats, "PLAYER STATS");
            if (!showPlayerStats) return;

            if (showAllPlayers)
            {
                for (int i = 0; i < playerList.Count; i++)
                    DrawOnePlayer(playerList[i], i);
            }
            else
            {
                DrawOnePlayer(GetSelectedPlayer(), selectedPlayerIndex);
            }
        }

        private void DrawOnePlayer(StatsController player, int index)
        {
            if (player == null || player.StatModule == null) return;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"PLAYER {index + 1}: {player.name}", EditorStyles.boldLabel);

            foreach (var pair in player.StatModule.GetAllStats())
            {
                var stat = pair.Key;
                var runtime = pair.Value;

                string key = $"{player.GetInstanceID()}_{stat}";
                bool fold = statFoldouts.ContainsKey(key) && statFoldouts[key];

                fold = EditorGUILayout.Foldout(fold,
                    $"{stat} | {runtime.BaseValue} → {runtime.FinalValue}");

                statFoldouts[key] = fold;

                if (fold)
                {
                    float flat = service.CurrentSnapshot.GetFlat(stat);
                    float percent = service.CurrentSnapshot.GetPercentOfBase(stat);

                    DrawRow("Flat", flat.ToString());
                    DrawRow("%Base", $"{percent * 100f:0.##}%");
                }
            }

            EditorGUILayout.EndVertical();
        }

        // =========================
        // MODIFIERS
        // =========================
        private void DrawModifiers()
        {
            showStatModuleModifiers = EditorGUILayout.Foldout(showStatModuleModifiers, "MODIFIERS");
            if (!showStatModuleModifiers) return;

            foreach (var pair in service.RawModifiersBySource)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField(pair.Key, EditorStyles.boldLabel);

                foreach (var m in pair.Value)
                {
                    DrawRow(m.TargetStat.ToString(),
                        m.ValueKind == PowerUpValueKind.PercentOfBase
                            ? $"{m.Value * 100f:0.##}%"
                            : m.Value.ToString());
                }

                EditorGUILayout.EndVertical();
            }
        }

        // =========================
        // UTILS
        // =========================
        private void DrawRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif