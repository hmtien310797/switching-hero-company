using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] TMP_Text goldText;
        [SerializeField] RectTransform uiCoinRect;

        private Canvas canvas;
        private Camera cam;

        public Vector3 GetCoinPointPos(float deepth)
        {
            if (canvas == null)
            {
                canvas = goldText.rectTransform.GetComponentInParent<Canvas>();
                cam = canvas.worldCamera;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, uiCoinRect.position);
            return new Vector3(screenPoint.x/Screen.width, screenPoint.y/Screen.height, deepth);
        }
    }
}
