using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.MissionSystem.Interfaces
{
    internal interface IMissionSystemService
    {
        /// <summary>
        /// cap nhat progress mission
        /// </summary>
        /// <param name="eventKey">loai nhiem vu can tang tien du lieu</param>
        /// <param name="value">so luong can cap nhat.</param>
        /// <returns>true neu cap nhat thanh cong, otherwise false.</returns>
        void ChangeProgress(string eventKey, int value);

        /// <summary>
        /// hoàn thành mission hien tai. va chuyen sang mission moi.
        /// </summary>
        /// <param name="cfg">nhiem vu tiep theo</param>
        void NextMission(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg);

        /// <summary>
        /// set trang thai claim
        /// </summary>
        /// <param name="missionId">loai nhiem vu can set</param>
        /// <param name="type">type nhiem vu</param>
        /// <param name="isClaimed">gia tri can set</param>
        void SetIsClaimed(string missionId, string type, bool isClaimed);
    }
}