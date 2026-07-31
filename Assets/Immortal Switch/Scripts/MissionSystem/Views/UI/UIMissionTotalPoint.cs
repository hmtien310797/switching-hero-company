using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionTotalPoint : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private TextMeshProUGUI txtPoint;

        [SerializeField]
        private TextMeshProUGUI txtTitle;

        public void Bind(int point, string missionType)
        {
            txtPoint.SetText(point.ToString());
            txtTitle.SetText(_GetTitle(missionType));
        }

        private string _GetTitle(string missionType)
        {
            return missionType switch
            {
                MissionTypes.WEEKLY => LocalizationManager.GetText(LocalizationKeys.UI_MISSION_WEEKLY_DECS),
                MissionTypes.DAILY => LocalizationManager.GetText(LocalizationKeys.UI_MISSION_DAILY_DECS2),
                _ => string.Empty,
            };
        }
    }
}