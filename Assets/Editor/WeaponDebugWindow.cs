#if UNITY_EDITOR
using System.Collections.Generic;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Editor
{
    public class WeaponDebugWindow : EditorWindow
    {
        private Vector2 scroll;

        private int selectedHeroId;
        private HeroDataSO selectedHero;
        private HeroClass selectedHeroClass;

        private StandardWeaponDefinitionSO selectedStandard;
        private List<StandardWeaponDefinitionSO> standardCandidates = new();

        private int addStandardShardAmount = 1;
        private int setStandardLevel = 1;
        private int setStandardLimitBreak = 0;

        private int addExclusiveShardAmount = 1;
        private int setExclusiveLevel = 1;
        private int setExclusiveLimitBreak = 0;
        private int setExclusiveStar = 1;

        private int grantWeaponEnhancementStoneAmount = 10000;
        private int grantWeaponBreakThroughStoneAmount = 10000;

        [MenuItem("Tools/Debug Window/Equipment/Weapon Debug Window")]
        public static void Open()
        {
            GetWindow<WeaponDebugWindow>("Weapon Debug");
        }

        private void OnEnable()
        {
            RefreshHeroSelection();
        }

        private void OnFocus()
        {
            RefreshHeroSelection();
        }

        private void OnGUI()
        {
            if (WeaponManager.Instance == null)
            {
                EditorGUILayout.HelpBox("WeaponManager not found in scene.", MessageType.Warning);
                return;
            }

            if (DatabaseManager.Instance == null)
            {
                EditorGUILayout.HelpBox("DatabaseManager not found in scene.", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawHeroSelection();
            EditorGUILayout.Space(8);

            if (selectedHero == null)
            {
                EditorGUILayout.HelpBox("Please select a valid hero.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawHeroSummary();
            EditorGUILayout.Space(8);

            DrawStandardSection();
            EditorGUILayout.Space(8);

            DrawExclusiveSection();
            EditorGUILayout.Space(8);

            DrawEquipSection();
            EditorGUILayout.Space(8);

            DrawCurrencySection();
            EditorGUILayout.Space(8);

            DrawRuntimePreview();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeroSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("HERO SELECTION", EditorStyles.boldLabel);

            int newHeroId = EditorGUILayout.IntField("Hero Id", selectedHeroId);
            if (newHeroId != selectedHeroId)
            {
                selectedHeroId = newHeroId;
                RefreshHeroSelection();
            }

            if (GUILayout.Button("Refresh Hero"))
            {
                RefreshHeroSelection();
            }

            EditorGUILayout.EndVertical();
        }

        private void RefreshHeroSelection()
        {
            if (DatabaseManager.Instance == null)
                return;

            selectedHero = DatabaseManager.Instance.GetHeroDataById(selectedHeroId);
            if (selectedHero != null)
            {
                selectedHeroClass = selectedHero.HeroClass;
                standardCandidates = DatabaseManager.Instance.GetWeaponDatabase().GetStandardsByClass(selectedHeroClass);
                if (standardCandidates.Count > 0 && (selectedStandard == null || selectedStandard.WeaponClass != selectedHeroClass))
                    selectedStandard = standardCandidates[0];
            }
            else
            {
                selectedHeroClass = default;
                standardCandidates.Clear();
                selectedStandard = null;
            }
        }

        private void DrawHeroSummary()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("HERO SUMMARY", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Hero Name", selectedHero.Name);
            EditorGUILayout.LabelField("Hero Class", selectedHeroClass.ToString());

            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(selectedHeroId);
            EditorGUILayout.LabelField("Equipped Standard", equip.EquippedStandardWeaponId.ToString());
            EditorGUILayout.LabelField("Equipped Exclusive", equip.EquippedExclusiveWeaponId.ToString());
            EditorGUILayout.LabelField("Use Exclusive", equip.UseExclusive.ToString());

            EditorGUILayout.EndVertical();
        }

        private void DrawStandardSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("STANDARD WEAPON", EditorStyles.boldLabel);

            if (standardCandidates == null || standardCandidates.Count == 0)
            {
                EditorGUILayout.HelpBox("No standard weapon candidates for this class.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            string[] options = new string[standardCandidates.Count];
            int selectedIndex = 0;

            for (int i = 0; i < standardCandidates.Count; i++)
            {
                var def = standardCandidates[i];
                options[i] = $"{def.WeaponId} | {def.WeaponName} | {def.Tier}{def.Star}";
                if (selectedStandard != null && selectedStandard.WeaponId == def.WeaponId)
                    selectedIndex = i;
            }

            int newIndex = EditorGUILayout.Popup("Selected Standard", selectedIndex, options);
            selectedStandard = standardCandidates[newIndex];

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(selectedStandard.WeaponId);

            EditorGUILayout.LabelField("Unlocked", state.IsUnlocked.ToString());
            EditorGUILayout.LabelField("Level", state.Level.ToString());
            EditorGUILayout.LabelField("Limit Break", state.LimitBreakStage.ToString());
            EditorGUILayout.LabelField("Shard", state.CurrentShard.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Standard"))
            {
                WeaponManager.Instance.UnlockStandard(selectedStandard.WeaponId);
            }

            if (GUILayout.Button("Equip Standard"))
            {
                WeaponManager.Instance.EquipStandardAsync(selectedHeroId, selectedStandard.WeaponId).Forget();
            }
            EditorGUILayout.EndHorizontal();

            addStandardShardAmount = EditorGUILayout.IntField("Add Standard Shard", addStandardShardAmount);
            if (GUILayout.Button("Apply Standard Shard"))
            {
                WeaponManager.Instance.AddStandardShard(selectedStandard.WeaponId, addStandardShardAmount);
            }

            setStandardLevel = EditorGUILayout.IntField("Set Standard Level", setStandardLevel);
            if (GUILayout.Button("Apply Standard Level"))
            {
                WeaponManager.Instance.DebugSetStandardLevel(selectedStandard.WeaponId, setStandardLevel);
            }

            setStandardLimitBreak = EditorGUILayout.IntField("Set Standard LimitBreak", setStandardLimitBreak);
            if (GUILayout.Button("Apply Standard LimitBreak"))
            {
                WeaponManager.Instance.DebugSetStandardLimitBreakStage(selectedStandard.WeaponId, setStandardLimitBreak);
            }

            if (GUILayout.Button("Try Level Up Standard"))
            {
                WeaponManager.Instance.TryLevelUpStandardAsync(selectedStandard.WeaponId).Forget();
            }

            if (GUILayout.Button("Try Limit Break Standard"))
            {
                LogLimitBreakStandardAsync(selectedStandard.WeaponId).Forget();
            }

            if (GUILayout.Button("Try Fuse Standard"))
            {
                var result = WeaponManager.Instance.TryFuseStandard(selectedStandard.WeaponId);
                Debug.Log($"[WeaponDebug] Fuse Result = Success:{result.Success}, TargetStd:{result.TargetStandardWeaponId}, TargetEx:{result.TargetExclusiveWeaponId}");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawExclusiveSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("EXCLUSIVE WEAPON", EditorStyles.boldLabel);

            var def = DatabaseManager.Instance.GetWeaponDatabase().GetExclusiveByHeroId(selectedHeroId);
            if (def == null)
            {
                EditorGUILayout.HelpBox("No exclusive weapon for this hero.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, selectedHeroId);

            EditorGUILayout.LabelField("Exclusive Id", def.ExclusiveWeaponId.ToString());
            EditorGUILayout.LabelField("Exclusive Name", def.WeaponName);
            EditorGUILayout.LabelField("Unlocked", state.IsUnlocked.ToString());
            EditorGUILayout.LabelField("Level", state.Level.ToString());
            EditorGUILayout.LabelField("Limit Break", state.LimitBreakStage.ToString());
            EditorGUILayout.LabelField("Shard", state.CurrentShard.ToString());
            EditorGUILayout.LabelField("Star", state.CurrentStar.ToString());

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Exclusive"))
            {
                WeaponManager.Instance.UnlockExclusive(selectedHeroId);
            }

            if (GUILayout.Button("Equip Exclusive"))
            {
                WeaponManager.Instance.EquipExclusiveAsync(selectedHeroId).Forget();
            }
            EditorGUILayout.EndHorizontal();

            addExclusiveShardAmount = EditorGUILayout.IntField("Add Exclusive Shard", addExclusiveShardAmount);
            if (GUILayout.Button("Apply Exclusive Shard"))
            {
                WeaponManager.Instance.AddExclusiveShard(selectedHeroId, addExclusiveShardAmount);
            }

            setExclusiveLevel = EditorGUILayout.IntField("Set Exclusive Level", setExclusiveLevel);
            if (GUILayout.Button("Apply Exclusive Level"))
            {
                WeaponManager.Instance.DebugSetExclusiveLevel(selectedHeroId, setExclusiveLevel);
            }

            setExclusiveLimitBreak = EditorGUILayout.IntField("Set Exclusive LimitBreak", setExclusiveLimitBreak);
            if (GUILayout.Button("Apply Exclusive LimitBreak"))
            {
                WeaponManager.Instance.DebugSetExclusiveLimitBreakStage(selectedHeroId, setExclusiveLimitBreak);
            }

            setExclusiveStar = EditorGUILayout.IntField("Set Exclusive Star", setExclusiveStar);
            if (GUILayout.Button("Apply Exclusive Star"))
            {
                WeaponManager.Instance.DebugSetExclusiveStar(selectedHeroId, setExclusiveStar);
            }

            if (GUILayout.Button("Try Level Up Exclusive"))
            {
                WeaponManager.Instance.TryLevelUpExclusiveAsync(selectedHeroId).Forget();
            }

            if (GUILayout.Button("Try Limit Break Exclusive"))
            {
                LogLimitBreakExclusiveAsync(selectedHeroId).Forget();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEquipSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("AUTO / REFRESH", EditorStyles.boldLabel);

            if (GUILayout.Button("Try Auto Equip"))
            {
                WeaponManager.Instance.TryAutoEquipAsync(selectedHeroId, selectedHeroClass).Forget();
            }

            if (GUILayout.Button("Refresh Hero Equipment Runtime"))
            {
                WeaponManager.Instance.NotifyHeroWeaponChanged(selectedHeroId);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrencySection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("CURRENCY (DEBUG)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Level Up giờ dùng weapon_ore (server trừ thật qua weapon/upgrade, player_defaults.js " +
                "đã cấp sẵn 9999 lúc tạo account mới — chỉ cần grant tay nếu test account cũ đã hết). " +
                "WeaponBreakThroughStone (Limit Break) vẫn chưa có nguồn cấp thật, grant tạm để test.",
                MessageType.Info);

            grantWeaponEnhancementStoneAmount = EditorGUILayout.IntField("Grant weapon_ore", grantWeaponEnhancementStoneAmount);
            if (GUILayout.Button("Grant weapon_ore"))
            {
                WeaponManager.Instance.DebugAddWeaponCurrency(CurrencyType.weapon_ore, grantWeaponEnhancementStoneAmount);
            }

            grantWeaponBreakThroughStoneAmount = EditorGUILayout.IntField("Grant BreakThrough Stone", grantWeaponBreakThroughStoneAmount);
            if (GUILayout.Button("Grant BreakThrough Stone"))
            {
                WeaponManager.Instance.DebugAddWeaponCurrency(CurrencyType.WeaponBreakThroughStone, grantWeaponBreakThroughStoneAmount);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimePreview()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("RUNTIME PREVIEW", EditorStyles.boldLabel);

            var heroes = GameObject.FindObjectsOfType<HeroActor>();
            HeroActor runtimeHero = null;

            for (int i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null && heroes[i].GetHeroId() == selectedHeroId)
                {
                    runtimeHero = heroes[i];
                    break;
                }
            }

            if (runtimeHero == null || runtimeHero.Stats == null || runtimeHero.Stats.StatModule == null)
            {
                EditorGUILayout.HelpBox("Runtime hero not found in active scene.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var module = runtimeHero.Stats.StatModule;

            EditorGUILayout.LabelField("Final Atk", module.GetFinalStat(StatType.Atk).ToString("0.##"));
            EditorGUILayout.LabelField("Final MaxHp", module.GetFinalStat(StatType.MaxHp).ToString("0.##"));
            EditorGUILayout.LabelField("Final Def", module.GetFinalStat(StatType.Def).ToString("0.##"));
            EditorGUILayout.LabelField("Final CritChance", module.GetFinalStat(StatType.CritChance).ToString("0.##"));

            EditorGUILayout.EndVertical();
        }

        private async UniTask LogLimitBreakStandardAsync(int weaponId)
        {
            var result = await WeaponManager.Instance.TryLimitBreakStandardAsync(weaponId);
            Debug.Log($"[WeaponDebug] Standard LimitBreak Result = {result}");
        }

        private async UniTask LogLimitBreakExclusiveAsync(int heroId)
        {
            var result = await WeaponManager.Instance.TryLimitBreakExclusiveAsync(heroId);
            Debug.Log($"[WeaponDebug] Exclusive LimitBreak Result = {result}");
        }
    }
}
#endif