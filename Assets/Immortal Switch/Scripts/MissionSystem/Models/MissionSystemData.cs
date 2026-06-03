using System.Collections.Generic;
using System.Linq;

namespace Immortal_Switch.Scripts.MissionSystem.Models
{
    /// <summary>
    /// Các loại nhiệm vụ trong game.
    /// </summary>
    public enum EMissionSystemType
    {
        /// <summary>
        /// Tiêu diệt quái vật.
        /// </summary>
        KillMonsters,

        /// <summary>
        /// Thực hiện transmute / hợp thành.
        /// </summary>
        Transmute,

        /// <summary>
        /// Triệu hồi hero.
        /// </summary>
        SummonHeroes,

        /// <summary>
        /// Triệu hồi kỹ năng.
        /// </summary>
        SummonSkills,

        /// <summary>
        /// Đạt tới stage yêu cầu.
        /// </summary>
        ReachStage,

        /// <summary>
        /// Đạt tới cấp training HP yêu cầu.
        /// </summary>
        ReachTrainingHp,

        /// <summary>
        /// Đạt tới cấp training ATK yêu cầu.
        /// </summary>
        ReachTrainingAtk,

        /// <summary>
        /// Thay đổi hero.
        /// </summary>
        SwitchHero,

        /// <summary>
        /// Sử dụng kỹ năng.
        /// </summary>
        UseSkill,
    }

    /// <summary>
    /// Dữ liệu tiến trình nhiệm vụ của người chơi.
    /// </summary>
    public class MissionSystemData
    {
        /// <summary>
        /// Ds các nhiệm vụ của user.
        /// key: mission type
        /// value: ds mission của type đó
        /// </summary>
        public Dictionary<string, List<MissionSystemEntry>> Missions = new();
    }

    /// <summary>
    /// Dữ liệu tiến trình nhiệm vụ của người chơi.
    /// </summary>
    public class MissionSystemEntry
    {
        /// <summary>
        /// Id nhiệm vụ trong config.
        /// </summary>
        public string Id;

        /// <summary>
        /// Loại nhiệm vụ của id.
        /// </summary>
        public string EventKey;

        /// <summary>
        /// Tiến độ hiện tại của nhiệm vụ.
        /// </summary>
        public int Progress;

        /// <summary>
        /// Đã nhận thưởng.
        /// </summary>
        public bool IsClaimed;

        public void SetProgress(int value)
        {
            Progress = value;
        }

        public void SetId(string id)
        {
            Id = id;
        }

        public void SetIsClaimed(bool value)
        {
            IsClaimed = value;
        }
    }
}