using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    [CreateAssetMenu(fileName = "SkillSummonRarityVisualConfig", menuName = "Game/Skill Summon/Rarity Visual Config")]
    public class SkillSummonRarityVisualConfigSO : ScriptableObject
    {
        public List<SkillSummonRarityVisualEntry> entries = new();

        public SkillSummonRarityVisualEntry Get(SkillSummonGrade grade)
        {
            return entries.FirstOrDefault(x => x != null && x.grade == grade);
        }
    }

    [Serializable]
    public class SkillSummonRarityVisualEntry
    {
        public SkillSummonGrade grade;
        public Sprite icon;
        public Color topColor = Color.white;
        public Color bottomColor = Color.white;
    }
}