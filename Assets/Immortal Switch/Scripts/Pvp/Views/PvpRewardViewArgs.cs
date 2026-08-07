namespace Immortal_Switch.Scripts.Pvp.Views
{
    /// <summary>
    /// Args truyền vào <see cref="PvpRewardView"/> khi mở (vd từ PvpRankSeasonView). RewardStr theo
    /// format "itemId:qty;itemId:qty;..." (vd "3:1000;1:5000").
    /// </summary>
    public sealed class PvpRewardViewArgs
    {
        public string Title;      // vd "Bronze 1"
        public string RewardStr;  // vd "3:1000;1:5000"
    }
}