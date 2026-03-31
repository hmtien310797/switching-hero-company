using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    [CreateAssetMenu(fileName = "HeroSkillLoadoutInit", menuName = "ScriptableObjects/HeroSkillLoadoutInit")]
    public class HeroSkillLoadoutInitSO : ScriptableObject
    {
        public List<HeroSkillLoadoutEntry> HeroEntries = new();
    }

    [Serializable]
    public class HeroSkillLoadoutEntry
    {
        public int HeroId;
        public List<int> EquippedSkillIds = new();
    }
}