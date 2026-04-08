using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Hero;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class HeroProgressionDebugWindow : EditorWindow
    {
        private HeroProgressionDatabaseSO database;
        private Vector2 scrollPos;

        private int selectedHeroIndex = 0;
        private int shardAmount = 1;

        private readonly List<HeroDataSO> validHeroes = new();
        private string[] heroOptions = new string[0];

        [MenuItem("Tools/Hero/Hero Progression Debug Window")]
        public static void Open()
        {
            GetWindow<HeroProgressionDebugWindow>("Hero Progression Debug");
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Hero Progression Debug", EditorStyles.boldLabel);

            database = (HeroProgressionDatabaseSO)EditorGUILayout.ObjectField(
                "Hero Database",
                database,
                typeof(HeroProgressionDatabaseSO),
                false);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Hãy Play game để chỉnh data runtime và thấy UI update ngay.", MessageType.Warning);
                return;
            }

            if (HeroProgressionManager.Instance == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy HeroProgressionManager trong scene.", MessageType.Error);
                return;
            }

            if (database == null)
            {
                EditorGUILayout.HelpBox("Hãy gán HeroProgressionDatabaseSO.", MessageType.Warning);
                return;
            }

            BuildHeroOptions();

            if (validHeroes.Count == 0)
            {
                EditorGUILayout.HelpBox("Database không có hero hợp lệ.", MessageType.Warning);
                return;
            }

            selectedHeroIndex = Mathf.Clamp(selectedHeroIndex, 0, validHeroes.Count - 1);
            selectedHeroIndex = EditorGUILayout.Popup("Hero", selectedHeroIndex, heroOptions);

            shardAmount = EditorGUILayout.IntField("Shard Amount", shardAmount);
            shardAmount = Mathf.Max(1, shardAmount);

            var selectedHero = validHeroes[selectedHeroIndex];

            GUILayout.Space(8);
            DrawSelectedHeroInfo(selectedHero);

            GUILayout.Space(8);
            GUILayout.Label("Actions", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Acquire Hero"))
            {
                HeroProgressionManager.Instance.AcquireHeroIfNeeded(selectedHero);
            }

            if (GUILayout.Button("Add 1 Shard"))
            {
                HeroProgressionManager.Instance.AddShardToHero(selectedHero, 1, true);
            }

            if (GUILayout.Button($"Add {shardAmount} Shard"))
            {
                HeroProgressionManager.Instance.AddShardToHero(selectedHero, shardAmount, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Upgrade Hero"))
            {
                HeroProgressionManager.Instance.UpgradeHero(selectedHero.Id);
            }

            if (GUILayout.Button("Reset This Hero"))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Reset hero {selectedHero.Name}?", "Yes", "No"))
                {
                    HeroProgressionManager.Instance.ResetHero(selectedHero.Id);
                }
            }

            if (GUILayout.Button("Reset Progress (Keep Unlock)"))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Reset progress of hero {selectedHero.Name} but keep acquired?", "Yes", "No"))
                {
                    HeroProgressionManager.Instance.ResetHeroProgress(selectedHero.Id);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("CLEAR ALL HERO DATA"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Clear ALL hero progression data?", "Yes", "No"))
                {
                    HeroProgressionManager.Instance.ClearAllHeroData();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(12);
            GUILayout.Label("All Heroes", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < validHeroes.Count; i++)
            {
                DrawMiniHeroRow(validHeroes[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void BuildHeroOptions()
        {
            validHeroes.Clear();

            if (database == null || database.Heroes == null)
            {
                heroOptions = new string[0];
                return;
            }

            validHeroes.AddRange(database.Heroes.Where(x => x != null).OrderBy(x => x.Id));
            heroOptions = validHeroes.Select(x => $"{x.Id} - {x.Name}").ToArray();
        }

        private void DrawSelectedHeroInfo(HeroDataSO hero)
        {
            var service = HeroProgressionManager.Instance.Service;
            var owned = service.GetOrCreateOwnedHero(hero.Id);

            if (!owned.IsUnlocked)
            {
                var cfg = database.GetProgressionConfig(hero.Id);
                string startTier = cfg != null ? cfg.StartingTier.ToString() : "Unknown";
                EditorGUILayout.HelpBox($"Not Acquired | Start Tier: {startTier}", MessageType.Info);
                return;
            }

            var node = service.GetCurrentNode(hero.Id);
            int maxStar = service.GetMaxStarInCurrentTier(hero.Id);
            int costToNext = (node == null || node.IsMaxNode) ? 0 : node.ShardCostToNext;

            EditorGUILayout.HelpBox(
                $"Acquired | Tier: {owned.CurrentTier} | Star: {owned.CurrentStarInTier}/{maxStar} | Shard: {owned.CurrentShard} | CostToNext: {costToNext}",
                MessageType.None);
        }

        private void DrawMiniHeroRow(HeroDataSO hero)
        {
            var service = HeroProgressionManager.Instance.Service;
            var owned = service.GetOrCreateOwnedHero(hero.Id);

            GUILayout.BeginHorizontal("box");

            GUILayout.Label($"{hero.Id} - {hero.Name}", GUILayout.Width(180));

            if (!owned.IsUnlocked)
            {
                GUILayout.Label("Not Acquired", GUILayout.Width(120));
            }
            else
            {
                int maxStar = service.GetMaxStarInCurrentTier(hero.Id);
                GUILayout.Label($"Tier: {owned.CurrentTier}", GUILayout.Width(110));
                GUILayout.Label($"Star: {owned.CurrentStarInTier}/{maxStar}", GUILayout.Width(80));
                GUILayout.Label($"Shard: {owned.CurrentShard}", GUILayout.Width(80));
            }

            if (GUILayout.Button("+1", GUILayout.Width(40)))
            {
                HeroProgressionManager.Instance.AddShardToHero(hero, 1, true);
            }

            if (GUILayout.Button("Upgrade", GUILayout.Width(70)))
            {
                HeroProgressionManager.Instance.UpgradeHero(hero.Id);
            }

            GUILayout.EndHorizontal();
        }
    }
}