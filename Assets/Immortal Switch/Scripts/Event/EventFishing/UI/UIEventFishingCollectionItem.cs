using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventFishing.UI
{
    public class UIEventFishingCollectionItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtNo;

        [SerializeField]
        private GameObject goUnclaimed;

        public void Bind(int idx, bool claimed)
        {
            txtNo.text = $"No.{idx}";

            goUnclaimed.SetActive(!claimed);
        }
    }
}