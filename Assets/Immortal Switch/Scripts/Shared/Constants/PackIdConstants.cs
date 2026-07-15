namespace Immortal_Switch.Scripts.Shared.Constants
{
    public class PackIdConstants
    {
        /// <summary>
        /// goi dac biet tuan 7 ngay
        /// </summary>
        public const int ID_WEEKLY_SPECIAL = 13;

        /// <summary>
        /// goi dac biet 1n
        /// </summary>
        public const int ID_DAILY_SPECIAL = 14;

        /// <summary>
        /// goi thang thuong
        /// </summary>
        public const int ID_MONTHLY_NORMAL = 15;

        /// <summary>
        /// goi thang cao
        /// </summary>
        public const int ID_MONTHLY_PREMIUM = 16;

        public static bool IsPackSpecial(int packId)
        {
            return packId is ID_DAILY_SPECIAL or ID_WEEKLY_SPECIAL or ID_MONTHLY_NORMAL or ID_MONTHLY_PREMIUM;
        }
    }
}