using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem.Interfaces
{
    internal interface IMissionSystemService
    {
        /// <summary>
        /// cap nhat progress mission
        /// </summary>
        /// <param name="eventKey">loai nhiem vu can tang tien du lieu</param>
        /// <param name="value">so luong can cap nhat.</param>
        /// <returns>ds nhiem vu da duoc cap nhat</returns>
        Dictionary<string, MissionSystemEntry> ChangeProgress(string eventKey, int value);

        /// <summary>
        /// hoàn thành mission hien tai. va chuyen sang mission moi.
        /// </summary>
        /// <param name="cfg">nhiem vu tiep theo</param>
        void NextMainMission(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg);

        /// <summary>
        /// set trang thai claim
        /// </summary>
        /// <param name="missionId">loai nhiem vu can set</param>
        /// <param name="missionType">loai nhiem vu dang can kiem tra</param>
        /// <param name="isClaimed">gia tri can set</param>
        bool SetIsClaimed(string missionId, string missionType, bool isClaimed);

        /// <summary>
        /// tang diem point
        /// </summary>
        /// <param name="missionType">loai nhiem vu can tang point</param>
        /// <param name="point">so point can tang</param>
        void IncreasePoint(string missionType, int point);

        /// <summary>
        /// nhiem vu da hoan thanh hay chua
        /// </summary>
        /// <param name="cfg">cfg</param>
        bool IsCompleted(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg);

        /// <summary>
        /// reward group claim
        /// </summary>
        /// <param name="row">cfg</param>
        /// <param name="isAdsX2">xem ads x2</param>
        List<RewardEntry> RewardGroupClaim(DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow row, bool isAdsX2);
    }
}