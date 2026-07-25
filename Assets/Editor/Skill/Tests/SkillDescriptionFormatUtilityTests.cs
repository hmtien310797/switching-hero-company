using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillSystem.Description;
using UnityEditor;
using UnityEngine;

namespace Editor.Skill.Tests
{
    /// <summary>
    /// Editor smoke test (MenuItem, không cần NUnit) cho:
    /// - <see cref="SkillDescriptionFormatUtility.GetRequiredArgumentCount"/> (parser).
    /// - <see cref="SkillDataSO.GetDefaultDescriptionValue"/> (mặc định 0/1/2).
    /// - <see cref="SkillDataSO.ResizeDescriptionValues"/> (resize giữ data cũ, pad theo level).
    /// Chạy qua menu Tools/Skill/Run Placeholder Parser Tests.
    /// </summary>
    public static class SkillDescriptionFormatUtilityTests
    {
        private const string MenuPath = "Tools/Skill/Run Placeholder Parser Tests";

        [MenuItem(MenuPath)]
        public static void RunTests()
        {
            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string message)
            {
                if (condition)
                {
                    passed++;
                    return;
                }

                failed++;
                Debug.LogError($"[SkillTests] FAIL {message}");
            }

            // --- Parser: GetRequiredArgumentCount ---
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("") == 0, "empty => 0");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount(null) == 0, "null => 0");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("No arguments") == 0, "no args => 0");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{0}") == 1, "{0} => 1");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{0} {1} {2}") == 3, "{0}{1}{2} => 3");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{2}") == 3, "{2} => 3");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{0} {0}") == 1, "{0}{0} => 1");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("<color=#FFF>{1}%</color>") == 2, "color{1} => 2");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{10}") == 11, "{10} => 11");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{hit} {finalhit}") == 0, "{hit} => 0");
            Assert(SkillDescriptionFormatUtility.GetRequiredArgumentCount("{{0}}") == 0, "escaped {{0}} => 0");

            // --- Default value by level (0/1/2) ---
            Assert(Mathf.Approximately(SkillDataSO.GetDefaultDescriptionValue(1), 0f), "default L1 = 0");
            Assert(Mathf.Approximately(SkillDataSO.GetDefaultDescriptionValue(2), 1f), "default L2 = 1");
            Assert(Mathf.Approximately(SkillDataSO.GetDefaultDescriptionValue(3), 2f), "default L3 = 2");
            Assert(Mathf.Approximately(SkillDataSO.GetDefaultDescriptionValue(99), 0f), "default L99 = 0");

            // --- Resize: grow giữ data cũ, pad theo level ---
            // Level 1 old [5], required 3 => [5, 0, 0]
            float[] l1 = { 5f };
            SkillDataSO.ResizeDescriptionValues(ref l1, 3, 1, false);
            Assert(l1 != null && l1.Length == 3, "L1 grow length 3");
            Assert(Mathf.Approximately(l1[0], 5f), "L1 keep [0]=5");
            Assert(Mathf.Approximately(l1[1], 0f), "L1 pad [1]=0");
            Assert(Mathf.Approximately(l1[2], 0f), "L1 pad [2]=0");

            // Level 2 old [5], required 3 => [5, 1, 1]
            float[] l2 = { 5f };
            SkillDataSO.ResizeDescriptionValues(ref l2, 3, 2, false);
            Assert(l2 != null && l2.Length == 3, "L2 grow length 3");
            Assert(Mathf.Approximately(l2[0], 5f), "L2 keep [0]=5");
            Assert(Mathf.Approximately(l2[1], 1f), "L2 pad [1]=1");
            Assert(Mathf.Approximately(l2[2], 1f), "L2 pad [2]=1");

            // Level 3 old [5], required 3 => [5, 2, 2]
            float[] l3 = { 5f };
            SkillDataSO.ResizeDescriptionValues(ref l3, 3, 3, false);
            Assert(l3 != null && l3.Length == 3, "L3 grow length 3");
            Assert(Mathf.Approximately(l3[0], 5f), "L3 keep [0]=5");
            Assert(Mathf.Approximately(l3[1], 2f), "L3 pad [1]=2");
            Assert(Mathf.Approximately(l3[2], 2f), "L3 pad [2]=2");

            // --- Resize: null Values, required 3 => toàn bộ theo default level ---
            float[] l1Null = null;
            SkillDataSO.ResizeDescriptionValues(ref l1Null, 3, 1, false);
            Assert(l1Null != null && l1Null.Length == 3, "L1 null grow length 3");
            Assert(Mathf.Approximately(l1Null[0], 0f) && Mathf.Approximately(l1Null[1], 0f) && Mathf.Approximately(l1Null[2], 0f), "L1 null all 0");

            float[] l3Null = null;
            SkillDataSO.ResizeDescriptionValues(ref l3Null, 3, 3, false);
            Assert(Mathf.Approximately(l3Null[0], 2f) && Mathf.Approximately(l3Null[1], 2f) && Mathf.Approximately(l3Null[2], 2f), "L3 null all 2");

            // --- Resize: Safe sync KHÔNG shrink (dài hơn required) ---
            float[] safe = { 1f, 2f, 3f, 4f };
            SkillDataSO.ResizeDescriptionValues(ref safe, 2, 1, false);
            Assert(safe != null && safe.Length == 4, "Safe sync keeps length 4");

            // --- Resize: Force match shrink ---
            float[] force = { 1f, 2f, 3f, 4f };
            SkillDataSO.ResizeDescriptionValues(ref force, 2, 1, true);
            Assert(force != null && force.Length == 2, "Force match shrinks to 2");
            Assert(Mathf.Approximately(force[0], 1f) && Mathf.Approximately(force[1], 2f), "Force match keeps first 2");

            // --- Format: mỗi level ra description khác nhau ---
            string template = "Fires for {0}s with {1} hits, each dealing {2}% ATK.";
            Assert(SkillDescriptionFormatUtility.FormatDescription(template, new[] { 2f, 5f, 100f }) == "Fires for 2s with 5 hits, each dealing 100% ATK.", "format L1");
            Assert(SkillDescriptionFormatUtility.FormatDescription(template, new[] { 3f, 7f, 125f }) == "Fires for 3s with 7 hits, each dealing 125% ATK.", "format L2");
            Assert(SkillDescriptionFormatUtility.FormatDescription(template, new[] { 4f, 9f, 150f }) == "Fires for 4s with 9 hits, each dealing 150% ATK.", "format L3");

            Debug.Log($"[SkillTests] Finished. Passed={passed}, Failed={failed}");

            if (failed == 0)
                Debug.Log("[SkillTests] All tests passed.");
        }
    }
}
