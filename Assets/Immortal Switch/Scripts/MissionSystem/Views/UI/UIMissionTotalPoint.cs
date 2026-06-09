using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionTotalPoint : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtPoint;

        [SerializeField] private TextMeshProUGUI txtTitle;

        public void Bind(int point, string missionType)
        {
            txtPoint.text = point.ToString();
            txtTitle.SetText(_GetTitle(missionType));
        }

        private string _GetTitle(string missionType)
        {
            switch (missionType)
            {
                case MissionSystemTypes.WEEKLY:
                    return "Điểm Nhiệm Vụ Hằng Tuần";

                case MissionSystemTypes.DAILY:
                    return "Điểm Nhiệm Vụ Hằng Ngày";
            }

            return string.Empty;
        }
    }
}