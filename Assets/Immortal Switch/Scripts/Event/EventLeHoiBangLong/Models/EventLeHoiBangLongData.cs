using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Models
{
    /// <summary>
    /// Lưu tiến độ và trạng thái nhận thưởng của một nhiệm vụ trong sự kiện.
    /// </summary>
    [Serializable]
    public class EventLeHoiBangLongMissionState
    {
        /// <summary>
        /// ID nhiệm vụ, tương ứng với missionId trong config nhiệm vụ sự kiện.
        /// </summary>
        public string missionId;

        /// <summary>
        /// Giá trị tiến độ hiện tại của nhiệm vụ.
        /// </summary>
        public int progress;

        /// <summary>
        /// Nhiệm vụ đã nhận thưởng hay chưa.
        /// </summary>
        public bool isClaimed;
    }

    /// <summary>
    /// Chứa toàn bộ dữ liệu tiến trình được lưu của sự kiện Lễ Hội Băng Long.
    /// </summary>
    [Serializable]
    public class EventLeHoiBangLongData
    {
        /// <summary>
        /// Số ngày người chơi đã đăng nhập trong event, tối đa bảy ngày.
        /// </summary>
        public int loginDay;

        /// <summary>
        /// Thời điểm UTC gần nhất đã được ghi nhận là một ngày đăng nhập.
        /// </summary>
        public DateTime? LastLoginDate;

        /// <summary>
        /// Danh sách ngày đã nhận phần thưởng đăng nhập miễn phí.
        /// </summary>
        public List<int> claimedFreeLoginDays = new();

        /// <summary>
        /// Danh sách ngày đã nhận lượt thưởng miễn phí riêng trong panel bonus.
        /// </summary>
        public List<int> claimedFreeBonusDays = new();

        /// <summary>
        /// Danh sách ngày đã mua gói phần thưởng bonus trả phí bằng IAP.
        /// </summary>
        public List<int> purchasedBonusDays = new();

        /// <summary>
        /// Danh sách ngày đã nhận phần thưởng từ gói bonus trả phí.
        /// </summary>
        public List<int> claimedBonusDays = new();

        /// <summary>
        /// Thời điểm UTC gần nhất đã reset tiến độ nhiệm vụ hằng ngày.
        /// </summary>
        public DateTime? LastMissionResetDate;

        /// <summary>
        /// Tổng điểm tích lũy từ các nhiệm vụ đã nhận thưởng trong event.
        /// </summary>
        public int missionPoints;

        /// <summary>
        /// Danh sách tiến độ và trạng thái của các nhiệm vụ hiện tại.
        /// </summary>
        public List<EventLeHoiBangLongMissionState> missions = new();

        /// <summary>
        /// Danh sách ID milestone điểm nhiệm vụ đã nhận thưởng.
        /// </summary>
        public List<int> claimedMissionMilestones = new();

        /// <summary>
        /// Tổng điểm tích lũy từ các lượt triệu hồi trong event.
        /// </summary>
        public int summonPoints;

        /// <summary>
        /// Danh sách ID milestone tích lũy triệu hồi đã nhận thưởng.
        /// </summary>
        public List<int> claimedSummonMilestones = new();
    }
}
