using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class ResponsivePanel : MonoBehaviour
    {
        public enum FitMode
        {
            FitInside,
            FitWidth,
            FitHeight
        }

        [SerializeField] private RectTransform panelRoot;

        [Header("Mode")]
        [SerializeField] private FitMode fitMode = FitMode.FitInside;
        [SerializeField] private bool useSafeArea = true;

        [Header("Auto Detect Design Size")]
        [SerializeField] private bool autoDetectDesignSize = true;
        [SerializeField] private Vector2 designSize = new Vector2(1300, 2200);

        [Header("Clamp")]
        [SerializeField] private float minScale = 0.35f;
        [SerializeField] private float maxScale = 1f;

        [Header("Optional Y Offset")]
        [SerializeField] private bool changePosY = false;
        [SerializeField] private float yPosPortrait = 0f;
        [SerializeField] private float yPosLandscape = 0f;

        private Vector2Int lastScreenSize;
        private Rect lastSafeArea;
        private bool cachedDesignSize;
        private const float defaultRatio = 2.05f;
        private const float defaultPortraitHeight = 1440f;

        private void OnEnable()
        {
            CacheDesignSize();
            Apply();
        }

        private void Update()
        {
            if (NeedRefresh())
            {
                Apply();
            }
        }

        private bool NeedRefresh()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                return true;

            if (useSafeArea && lastSafeArea != Screen.safeArea)
                return true;

            return false;
        }

        private void CacheDesignSize()
        {
            if (!autoDetectDesignSize || cachedDesignSize || panelRoot == null)
                return;

            // lấy size gốc (unscaled)
            Vector2 size = panelRoot.rect.size;

            // nếu bị scale rồi thì reverse lại
            if (panelRoot.localScale != Vector3.one)
            {
                size = new Vector2(
                    panelRoot.rect.width / panelRoot.localScale.x,
                    panelRoot.rect.height / panelRoot.localScale.y
                );
            }

            // fallback tránh lỗi
            if (size.x > 0 && size.y > 0)
            {
                designSize = size;
                cachedDesignSize = true;
            }
        }

        private void Apply()
        {
            if (panelRoot == null) return;

            bool isPortrait = Screen.height >= Screen.width;

            float scale = CalculateScale(isPortrait);

            panelRoot.localScale = Vector3.one * scale;

            if (changePosY)
            {
                Vector2 pos = panelRoot.anchoredPosition;
                pos.y = isPortrait ? yPosPortrait : yPosLandscape;
                panelRoot.anchoredPosition = pos;
            }

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
        }

        private float CalculateScale(bool isPortrait)
        {
            if (isPortrait)
                return 1f;

            Rect area = useSafeArea
                ? Screen.safeArea
                : new Rect(0, 0, Screen.width, Screen.height);

            float yOffset = 0f;

            if (panelRoot != null)
                yOffset = Mathf.Max(0f, panelRoot.anchoredPosition.y);

            float finalY = 1440;
            
            float currentRatio = Screen.width / Screen.height;
            bool isSameRatio = Mathf.Abs(currentRatio - defaultRatio) < 0.1f;
            
            if(area.height <= 1080)
            {
                finalY = 1440;
            }
            else if (area.height <= 1220)
            {
                finalY = 1320;
            }

            float widthRatio = area.width / designSize.x;
            float heightRatio = (finalY - yOffset)  / designSize.y;
            if (isSameRatio)
            {
                float newHeightRatio = defaultPortraitHeight / Screen.height;
                heightRatio -= newHeightRatio / 100;
            }

            float scale = 1f;

            switch (fitMode)
            {
                case FitMode.FitInside:
                    scale = Mathf.Min(widthRatio, heightRatio);
                    break;

                case FitMode.FitWidth:
                    scale = widthRatio;
                    break;

                case FitMode.FitHeight:
                    scale = heightRatio;
                    break;
            }

            return Mathf.Clamp(scale, minScale, maxScale);
        }
    }
}