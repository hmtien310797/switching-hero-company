#if UNITY_EDITOR
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp.DevTools;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Editor
{
    /// <summary>
    /// Editor tool (play-mode) để custom defender hero trong PvP hero-vs-hero như một user khác.
    /// Ghi vào <c>Assets/Resources/DefenderHeroTest.asset</c>; runtime đọc qua
    /// <see cref="DefenderHeroTestConfigSO.LoadOrCreate"/>. Tự lọc skill theo HeroClass và
    /// equipment theo class/hero để tránh config sai.
    /// <para>Growth/Transmutation (powerup) tạm bỏ qua.</para>
    /// </summary>
    public class DefenderHeroTestToolWindow : EditorWindow
    {
        private const string AssetPath = "Assets/Resources/DefenderHeroTest.asset";
        private const int ClassSkillSlotCount = 5;

        private DefenderHeroTestConfigSO config;
        private Vector2 scroll;

        [MenuItem("Tools/PvP/Defender Hero Test Tool")]
        public static void Open()
        {
            GetWindow<DefenderHeroTestToolWindow>("Defender Hero Test");
        }

        private void OnEnable()
        {
            config = LoadOrCreateAsset();
        }

        private void OnFocus()
        {
            if (config == null) config = LoadOrCreateAsset();
        }

        private void OnGUI()
        {
            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("Tool cần chạy trong play-mode (dùng DatabaseManager trong scene).", MessageType.Warning);
                return;
            }
            if (DatabaseManager.Instance == null)
            {
                EditorGUILayout.HelpBox("DatabaseManager not found in scene.", MessageType.Warning);
                return;
            }
            if (config == null) config = LoadOrCreateAsset();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("DEFENDER HERO TEST CONFIG", EditorStyles.boldLabel);
            config.Enabled = EditorGUILayout.Toggle("Enabled (dùng cho PvP next match)", config.Enabled);
            EditorGUILayout.HelpBox(
                "Khi Enabled, defender (đội AI) trong PvP hero-vs-hero sẽ được build từ config này " +
                "thay vì mock opponent random. Tắt để dùng mock như cũ.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            DrawSlot("FRONT DEFENDER", config.Front);
            EditorGUILayout.Space(12);
            DrawSlot("BACK DEFENDER", config.Back);
            EditorGUILayout.Space(12);

            EditorGUILayout.BeginVertical("box");
            if (GUILayout.Button("Save Config", GUILayout.Height(30)))
                Save();
            GUILayout.Label($"Saved at: {AssetPath}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSlot(string title, DefenderSlotConfig slot)
        {
            if (slot == null) return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            // Hero selection (IntField như WeaponDebugWindow).
            int newHeroId = EditorGUILayout.IntField("Hero Id", slot.HeroId);
            if (newHeroId != slot.HeroId)
            {
                slot.HeroId = newHeroId;
                slot.Skills.Clear();
            }

            var heroData = slot.HeroId > 0 ? DatabaseManager.Instance.GetHeroDataById(slot.HeroId) : null;
            if (heroData == null)
            {
                EditorGUILayout.HelpBox("HeroDataSO not found — chọn Hero Id hợp lệ.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("Name", heroData.Name);
            EditorGUILayout.LabelField("Class", heroData.HeroClass.ToString());

            slot.Tier = (HeroProgressTier)EditorGUILayout.EnumPopup("Tier", slot.Tier);
            int maxStar = GetMaxStarInTier(slot.HeroId, slot.Tier);
            slot.Star = EditorGUILayout.IntField($"Star (0-{Mathf.Max(0, maxStar)})", slot.Star);
            if (slot.Star < 0) slot.Star = 0;
            if (slot.Star > maxStar) slot.Star = maxStar;
            slot.Level = EditorGUILayout.IntField("Level", slot.Level);
            if (slot.Level < 1) slot.Level = 1;

            EditorGUILayout.Space(6);
            DrawSkills(slot, heroData.HeroClass);
            EditorGUILayout.Space(6);
            DrawEquipment(slot, heroData);

            EditorGUILayout.EndVertical();
        }

        private void DrawSkills(DefenderSlotConfig slot, HeroClass heroClass)
        {
            EditorGUILayout.LabelField("CLASS SKILLS (max 5)", EditorStyles.boldLabel);

            var allSkills = DatabaseManager.Instance.GetAllSkillData();
            var valid = new List<SkillDataSO>();
            if (allSkills != null)
            {
                for (int i = 0; i < allSkills.Count; i++)
                {
                    var s = allSkills[i];
                    if (s != null && s.SkillClass == heroClass)
                        valid.Add(s);
                }
            }

            if (valid.Count == 0)
            {
                EditorGUILayout.HelpBox($"Không có class skill nào cho {heroClass}.", MessageType.Info);
                return;
            }

            // Đảm bảo danh sách config có đủ 5 slot.
            while (slot.Skills.Count < ClassSkillSlotCount)
                slot.Skills.Add(new SkillEntry());
            while (slot.Skills.Count > ClassSkillSlotCount)
                slot.Skills.RemoveAt(slot.Skills.Count - 1);

            // Build options: "None" + valid skills. Đánh dấu skill đã dùng ở slot khác là disabled.
            string[] options = new string[valid.Count + 1];
            options[0] = "(none)";
            for (int i = 0; i < valid.Count; i++)
            {
                var s = valid[i];
                options[i + 1] = $"{s.SkillId} | {s.SkillKey} | lvlMax={s.MaxLevel}";
            }

            for (int slotIdx = 0; slotIdx < ClassSkillSlotCount; slotIdx++)
            {
                var entry = slot.Skills[slotIdx];
                int currentIdx = GetSkillOptionIndex(valid, entry.SkillId);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Slot {slotIdx + 1}", GUILayout.Width(48));

                int newIdx = EditorGUILayout.Popup(currentIdx, options);
                if (newIdx != currentIdx)
                {
                    entry.SkillId = newIdx == 0 ? 0 : valid[newIdx - 1].SkillId;
                    entry.SkillLevel = 1;
                    // Xoá skill vừa chọn khỏi các slot khác (tránh duplicate).
                    RemoveDuplicateSkill(slot, slotIdx, entry.SkillId);
                }

                if (entry.SkillId > 0)
                {
                    var skill = FindSkill(valid, entry.SkillId);
                    if (skill != null)
                    {
                        int maxLvl = Mathf.Max(1, skill.MaxLevel);
                        entry.SkillLevel = Mathf.Clamp(
                            EditorGUILayout.IntField(entry.SkillLevel, GUILayout.Width(44)),
                            1, maxLvl);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawEquipment(DefenderSlotConfig slot, HeroDataSO heroData)
        {
            EditorGUILayout.LabelField("EQUIPMENT", EditorStyles.boldLabel);

            var weaponDb = DatabaseManager.Instance.GetWeaponDatabase();
            var standardCandidates = weaponDb.GetStandardsByClass(heroData.HeroClass);
            var exclusive = weaponDb.GetExclusiveByHeroId(heroData.Id);

            slot.UseExclusive = EditorGUILayout.ToggleLeft("Use Exclusive Weapon", slot.UseExclusive);

            if (slot.UseExclusive)
            {
                if (exclusive == null)
                {
                    EditorGUILayout.HelpBox("Hero này không có exclusive weapon.", MessageType.Warning);
                    slot.UseExclusive = false;
                }
                else
                {
                    EditorGUILayout.LabelField($"Exclusive: {exclusive.ExclusiveWeaponId} | {exclusive.WeaponName}");
                    slot.ExclusiveWeaponId = exclusive.ExclusiveWeaponId;
                    slot.ExclusiveWeaponLevel = EditorGUILayout.IntField("Exclusive Level", slot.ExclusiveWeaponLevel);
                    if (slot.ExclusiveWeaponLevel < 1) slot.ExclusiveWeaponLevel = 1;
                    slot.ExclusiveWeaponStar = EditorGUILayout.IntField("Exclusive Star", slot.ExclusiveWeaponStar);
                    if (slot.ExclusiveWeaponStar < 1) slot.ExclusiveWeaponStar = 1;
                }
            }
            else
            {
                slot.ExclusiveWeaponId = 0;
                if (standardCandidates == null || standardCandidates.Count == 0)
                {
                    EditorGUILayout.HelpBox($"Không có standard weapon cho {heroData.HeroClass}.", MessageType.Info);
                }
                else
                {
                    string[] options = new string[standardCandidates.Count];
                    int selectedIdx = 0;
                    for (int i = 0; i < standardCandidates.Count; i++)
                    {
                        var w = standardCandidates[i];
                        options[i] = $"{w.WeaponId} | {w.WeaponName} | {w.Tier}{w.Star}";
                        if (slot.StandardWeaponId == w.WeaponId) selectedIdx = i;
                    }
                    int newIdx = EditorGUILayout.Popup("Standard Weapon", selectedIdx, options);
                    slot.StandardWeaponId = standardCandidates[newIdx].WeaponId;
                    slot.StandardWeaponLevel = EditorGUILayout.IntField("Standard Level", slot.StandardWeaponLevel);
                    if (slot.StandardWeaponLevel < 1) slot.StandardWeaponLevel = 1;
                }
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PvP] Defender Hero Test config saved.");
        }

        private static int GetSkillOptionIndex(List<SkillDataSO> valid, int skillId)
        {
            for (int i = 0; i < valid.Count; i++)
                if (valid[i].SkillId == skillId) return i + 1;
            return 0;
        }

        private static SkillDataSO FindSkill(List<SkillDataSO> valid, int skillId)
        {
            for (int i = 0; i < valid.Count; i++)
                if (valid[i].SkillId == skillId) return valid[i];
            return null;
        }

        private static void RemoveDuplicateSkill(DefenderSlotConfig slot, int targetSlot, int skillId)
        {
            if (skillId <= 0) return;
            for (int i = 0; i < slot.Skills.Count; i++)
            {
                if (i == targetSlot) continue;
                if (slot.Skills[i] != null && slot.Skills[i].SkillId == skillId)
                    slot.Skills[i].SkillId = 0;
            }
        }

        private static int GetMaxStarInTier(int heroId, HeroProgressTier tier)
        {
            if (heroId <= 0) return 0;
            try
            {
                var db = DatabaseManager.Instance?.HeroProgressionDatabase;
                var config = db != null ? db.GetProgressionConfig(heroId) : null;
                return config != null ? config.GetMaxStarInTier(tier) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static DefenderHeroTestConfigSO LoadOrCreateAsset()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<DefenderHeroTestConfigSO>(AssetPath);
            if (cfg != null) return cfg;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            cfg = ScriptableObject.CreateInstance<DefenderHeroTestConfigSO>();
            cfg.Front = new DefenderSlotConfig();
            cfg.Back = new DefenderSlotConfig();
            AssetDatabase.CreateAsset(cfg, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PvP] Created Defender Hero Test config at {AssetPath}");
            return cfg;
        }
    }
}
#endif