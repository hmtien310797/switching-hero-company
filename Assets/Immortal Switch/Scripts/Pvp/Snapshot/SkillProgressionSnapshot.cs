using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    [Serializable]
    public sealed class SkillSlotSnapshot
    {
        public int SkillId;
        public int Level;
        public string Grade;
    }

    /// <summary>
    /// Skill loadout fragment (DOCX §9). <b>FLAGGED:</b> inferred từ SkillModels (SkillInstance +
    /// SkillListResponse.Equipped). HeroUid → equipped SkillInstance[].
    /// </summary>
    [Serializable]
    public sealed class SkillProgressionSnapshot
    {
        public int HeroId;
        public string HeroUid;
        public List<SkillSlotSnapshot> Equipped = new();
    }
}
