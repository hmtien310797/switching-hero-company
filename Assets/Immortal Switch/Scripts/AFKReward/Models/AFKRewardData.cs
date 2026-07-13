using System;

namespace Immortal_Switch.Scripts.AFKReward.Models
{
    /// <summary>
    /// Dữ liệu AFK Reward — số lượt claim x2 trong ngày.
    /// </summary>
    public class AFKRewardData
    {
        /// <summary>
        /// Ngày gần nhất claim x2 (dùng để check daily reset).
        /// </summary>
        public DateTime? ClaimX2Date;

        /// <summary>
        /// Số lần đã claim x2 trong ngày hiện tại.
        /// </summary>
        public int ClaimX2Count;
    }
}
