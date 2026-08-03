namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Độ hiếm của Formation Buff (DOCX §21 "Rarity", §25 "Roll Configuration").
    /// Common/Uncommon/Rare/Epic/Legendary. Tỉ lệ rates là config-driven (SO), không hard-code.
    /// </summary>
    public enum BuffRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }
}
