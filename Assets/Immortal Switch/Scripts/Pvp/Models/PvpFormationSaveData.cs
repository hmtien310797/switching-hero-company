using System;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Loại slot buff trong 1 vị trí (DOCX §17 — "at most three buffs: Core, Support, and Trigger").
    /// </summary>
    public enum BuffSlotType
    {
        Core = 0,
        Support = 1,
        Trigger = 2
    }

    /// <summary>
    /// Loadout 3 slot buff của 1 vị trí. Mỗi slot chứa BuffId hoặc null/empty.
    /// </summary>
    [Serializable]
    public sealed class PvpBuffSlotLoadout
    {
        public string Core;
        public string Support;
        public string Trigger;

        public string Get(BuffSlotType slotType)
        {
            return slotType switch
            {
                BuffSlotType.Core => Core,
                BuffSlotType.Support => Support,
                BuffSlotType.Trigger => Trigger,
                _ => null
            };
        }

        public void Set(BuffSlotType slotType, string buffId)
        {
            switch (slotType)
            {
                case BuffSlotType.Core:
                    Core = buffId;
                    break;
                case BuffSlotType.Support:
                    Support = buffId;
                    break;
                case BuffSlotType.Trigger:
                    Trigger = buffId;
                    break;
            }
        }

        public void Clear()
        {
            Core = null;
            Support = null;
            Trigger = null;
        }
    }

    /// <summary>
    /// Đội hình PvP local: Front/Back hero + loadout buff từng vị trí.
    /// Key <see cref="PvpEs3Keys.Formation"/> (DOCX §6, §8, §17).
    /// </summary>
    [Serializable]
    public sealed class PvpFormationSaveData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;

        public int FrontHeroId = -1;
        public int BackHeroId = -1;

        public PvpBuffSlotLoadout FrontLoadout = new();
        public PvpBuffSlotLoadout BackLoadout = new();
    }
}
