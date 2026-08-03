using System;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>Args cho <see cref="PvpHeroSelectionView"/>: slot cần chọn + callback trả heroId.
    /// heroId = -1 nghĩa là huỷ chọn (DOCX §3 Markdown — hero đã gắn ở slot kia bị disable).</summary>
    public sealed class PvpHeroSelectionArgs
    {
        public FormationSlot Slot;
        public int OtherSlotHeroId;
        public Action<int> OnSelected;
    }

    /// <summary>Args cho <see cref="PvpBuffLoadoutView"/>: vị trí + slot type cần equip + working
    /// formation reference (equip mutate in-place) + callback refresh.</summary>
    public sealed class PvpBuffLoadoutArgs
    {
        public FormationSlot Slot;
        public BuffSlotType SlotType;
        public PvpFormationSaveData Formation;
        public Action OnEquipped;
    }
}
