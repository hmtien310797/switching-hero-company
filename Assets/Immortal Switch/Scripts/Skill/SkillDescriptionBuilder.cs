using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public static class SkillDescriptionBuilder
    {
        public static string BuildDescription(SkillDataSO skillData, int level)
        {
            if (skillData == null)
                return string.Empty;

            string result = skillData.Description;
            if (string.IsNullOrWhiteSpace(result))
                return string.Empty;

            int safeLevel = Mathf.Clamp(level, 1, Mathf.Max(1, skillData.MaxLevel));

            var levelData = skillData.GetLevelData(safeLevel);
            if (levelData == null)
                return result;

            // Ưu tiên DescriptionParams nếu có
            // if (levelData.DescriptionParams != null)
            // {
            //     for (int i = 0; i < levelData.DescriptionParams.Count; i++)
            //     {
            //         var param = levelData.DescriptionParams[i];
            //         if (param == null || string.IsNullOrEmpty(param.Key)) continue;
            //
            //         string valueText = FormatValue(param.Value, param.IsPercent, param.DecimalPlaces);
            //         result = result.Replace("{" + param.Key + "}", valueText);
            //     }
            // }

            //ReplaceAutoTokens(levelData, ref result);

            return result;
        }

        // private static void ReplaceAutoTokens(SkillLevelData levelData, ref string result)
        // {
        //     if (levelData.Phases == null) return;
        //
        //     int damageIndex = 1;
        //     int valueIndex = 1;
        //     int durationIndex = 1;
        //     int chanceIndex = 1;
        //
        //     for (int i = 0; i < levelData.Phases.Count; i++)
        //     {
        //         var phase = levelData.Phases[i];
        //         if (phase == null || phase.Effects == null) continue;
        //
        //         for (int j = 0; j < phase.Effects.Count; j++)
        //         {
        //             var effect = phase.Effects[j];
        //             if (effect == null) continue;
        //
        //             if (effect.EffectType == SkillEffectType.Damage)
        //             {
        //                 result = result.Replace("{damage" + damageIndex + "}", Mathf.RoundToInt(effect.DamageMultiplier).ToString());
        //                 result = result.Replace("{damage}", Mathf.RoundToInt(effect.DamageMultiplier).ToString());
        //                 damageIndex++;
        //             }
        //
        //             result = result.Replace("{value" + valueIndex + "}", Mathf.RoundToInt(effect.Value).ToString());
        //             result = result.Replace("{duration" + durationIndex + "}", effect.Duration.ToString("0.##"));
        //             result = result.Replace("{chance" + chanceIndex + "}", effect.ChancePercent.ToString("0.##"));
        //
        //             valueIndex++;
        //             durationIndex++;
        //             chanceIndex++;
        //         }
        //     }
        // }

        private static string FormatValue(float value, bool isPercent, int decimalPlaces)
        {
            string format = "F" + Mathf.Max(0, decimalPlaces);
            string text = value.ToString(format);
            return isPercent ? text + "%" : text;
        }
    }
}