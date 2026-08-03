namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Currency type cho PvP local economy. <b>FLAGGED:</b> DOCX §25 dùng tên <c>CurrencyType</c>;
    /// dùng <c>PvpCurrencyType</c> riêng để không coupling với shared CurrencyManager (PvP currency
    /// là local ES3, không qua CurrencyManager). Phase-2 server map lại nếu cần.
    /// </summary>
    public enum PvpCurrencyType
    {
        ArenaToken = 0,
        ArenaTicket = 1
    }
}
