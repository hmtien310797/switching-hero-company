using System;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Models;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Interfaces
{
    /// <summary>
    /// Xử lý nghiệp vụ và thay đổi tiến trình của sự kiện Lễ Hội Băng Long.
    /// </summary>
    public interface IEventLeHoiBangLongService
    {
        /// <summary>
        /// Khởi tạo dữ liệu, ghi nhận ngày đăng nhập và reset nhiệm vụ khi sang ngày mới.
        /// </summary>
        bool Initialize(DateTime utcNow);

        /// <summary>
        /// Ghi nhận một ngày đăng nhập mới, tối đa bảy ngày.
        /// </summary>
        bool RegisterLogin(DateTime utcNow);

        /// <summary>
        /// Kiểm tra và reset tiến độ nhiệm vụ hằng ngày, giữ nguyên tổng điểm và milestone.
        /// </summary>
        bool CheckDailyMissionReset(DateTime utcNow);

        /// <summary>
        /// Kiểm tra phần thưởng miễn phí của ngày có thể nhận hay không.
        /// </summary>
        bool CanClaimFreeLoginReward(int day);

        /// <summary>
        /// Đánh dấu phần thưởng miễn phí của ngày đã được nhận.
        /// </summary>
        bool ClaimFreeLoginReward(int day);

        /// <summary>
        /// Kiểm tra lượt thưởng miễn phí riêng trong panel bonus có thể nhận hay không.
        /// </summary>
        bool CanClaimFreeBonus(int day);

        /// <summary>
        /// Đánh dấu lượt thưởng miễn phí riêng trong panel bonus đã được nhận.
        /// </summary>
        bool ClaimFreeBonus(int day);

        /// <summary>
        /// Lấy trạng thái đã nhận lượt thưởng miễn phí riêng trong panel bonus.
        /// </summary>
        bool IsFreeBonusClaimed(int day);

        /// <summary>
        /// Kiểm tra gói bonus trả phí của ngày đã đủ điều kiện để mua bằng IAP hay chưa.
        /// </summary>
        bool CanPurchaseBonus(int day);

        /// <summary>
        /// Ghi nhận gói bonus trả phí đã mua sau khi IAP được xác nhận thành công.
        /// </summary>
        bool ConfirmBonusPurchase(int day);

        /// <summary>
        /// Kiểm tra phần thưởng bonus đã mua có thể nhận hay không.
        /// </summary>
        bool CanClaimBonus(int day);

        /// <summary>
        /// Đánh dấu phần thưởng bonus của ngày đã được nhận.
        /// </summary>
        bool ClaimBonus(int day);

        /// <summary>
        /// Lấy trạng thái đã nhận phần thưởng miễn phí của ngày.
        /// </summary>
        bool IsFreeLoginRewardClaimed(int day);

        /// <summary>
        /// Lấy trạng thái đã mua gói bonus của ngày.
        /// </summary>
        bool IsBonusPurchased(int day);

        /// <summary>
        /// Lấy trạng thái đã nhận phần thưởng bonus của ngày.
        /// </summary>
        bool IsBonusClaimed(int day);

        /// <summary>
        /// Cộng tiến độ cho tất cả nhiệm vụ có trigger tương ứng.
        /// </summary>
        bool ChangeMissionProgress(string trigger, int value);

        /// <summary>
        /// Lấy dữ liệu tiến độ của một nhiệm vụ theo ID.
        /// </summary>
        EventLeHoiBangLongMissionState GetMissionState(string missionId);

        /// <summary>
        /// Nhận thưởng nhiệm vụ đã hoàn thành và cộng điểm nhiệm vụ.
        /// </summary>
        bool ClaimMission(DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row);

        /// <summary>
        /// Kiểm tra milestone điểm nhiệm vụ đã nhận hay chưa.
        /// </summary>
        bool IsMissionMilestoneClaimed(int milestoneId);

        /// <summary>
        /// Nhận milestone khi tổng điểm nhiệm vụ đạt yêu cầu.
        /// </summary>
        bool ClaimMissionMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow row);

        /// <summary>
        /// Cộng điểm tích lũy triệu hồi của sự kiện.
        /// </summary>
        bool AddSummonPoints(int value);

        /// <summary>
        /// Kiểm tra milestone tích lũy triệu hồi đã nhận hay chưa.
        /// </summary>
        bool IsSummonMilestoneClaimed(int milestoneId);

        /// <summary>
        /// Nhận milestone khi điểm tích lũy triệu hồi đạt yêu cầu.
        /// </summary>
        bool ClaimSummonMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow row);
    }
}
