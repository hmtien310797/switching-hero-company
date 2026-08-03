using System;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>1 buff đã equip trong formation, snapshot cho battle (DOCX §9, §17).</summary>
    [Serializable]
    public sealed class FormationBuffSnapshot
    {
        public string BuffId;
        public int Level;
        public int BuffDataVersion = 1;
        public FormationSlot Slot;
        public BuffSlotType SlotType;
    }
}
