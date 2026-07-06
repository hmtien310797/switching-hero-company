using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class BaseUIPopup : MonoBehaviour
    {
        [Header("Close")] [SerializeField] private Button btnClose;

        protected virtual void BindButtons()
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(OnClickClose);
            }
        }

        private void OnClickClose()
        {
            gameObject.SetActive(false);
        }
    }
}