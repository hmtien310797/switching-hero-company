using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.UI
{
    public class ResponsivePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        
        [Header("Portrait")]
        [SerializeField] private float scalePortrait = 1f;
        [SerializeField] private float yPosPortrait = 1f;
        
        [Header("Landscape")]
        [SerializeField] private float scaleLandscape = 0.53f;
        [SerializeField] private float yPosLandscape = 1f;
        
        [SerializeField] private bool changePosY = false;

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
            panelRoot.localScale = isPortrait ? scalePortrait * Vector3.one : scaleLandscape * Vector3.one;
            if (!changePosY) return;
            panelRoot.anchoredPosition = new Vector2(panelRoot.anchoredPosition.x, isPortrait ? yPosPortrait : yPosLandscape);
            
        }
    }
}