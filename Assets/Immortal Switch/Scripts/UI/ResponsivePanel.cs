using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class ResponsivePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;

        [Header("Portrait")]
        [SerializeField] private Vector2 portraitSize = new Vector2(700f, 1500f);

        [Header("Landscape")]
        [SerializeField] private Vector2 landscapeSize = new Vector2(1200f, 700f);
        
        [SerializeField] private Vector2 startPosition = new Vector2(0, 0);

        private Vector2Int lastScreenSize;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            bool isPortrait = Screen.height >= Screen.width;

            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = startPosition;

            Vector2 size = isPortrait ? portraitSize : landscapeSize;
            panelRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            panelRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}