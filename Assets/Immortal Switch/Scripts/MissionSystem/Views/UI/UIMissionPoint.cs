using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionPoint : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtPoint;

        public void Bind(int point)
        {
            txtPoint.text = point.ToString();
        }
    }
}