using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    [CreateAssetMenu(fileName = "ClassSkillUnlockInit", menuName = "ScriptableObjects/ClassSkillUnlockInit")]
    public class ClassSkillUnlockInitSO : ScriptableObject
    {
        public List<ClassSkillUnlockEntry> ClassEntries = new();
    }

    [Serializable]
    public class ClassSkillUnlockEntry
    {
        public HeroClass HeroClass;
        public List<int> UnlockedSkillIds = new();
    }
}