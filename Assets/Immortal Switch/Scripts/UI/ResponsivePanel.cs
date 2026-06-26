using System.Collections;
using Sirenix.OdinInspector;
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

        [Required] [SerializeField] private RectTransform panelRoot;

        [Header("Mode")] [SerializeField] private FitMode fitMode = FitMode.FitInside;
        [SerializeField] private bool useSafeArea = true;

        /*[Header("Auto Detect Design Size")] [SerializeField]
        private bool autoDetectDesignSize = true;*/

        /*[SerializeField] private Vector2 designSize = new Vector2(1300, 2200);*/

        [Header("Clamp")] [SerializeField] private float minScale = 0.35f;
        [SerializeField] private float maxScale = 1f;

        [Header("Fixed anchored position, unity auto fill value")] [ReadOnly] [SerializeField]
        private float anchoredY;

        [Header("Optional Y Offset")] [SerializeField]
        private bool changePosY = false;

        [SerializeField] private float yPosPortrait = 0f;
        [SerializeField] private float yPosLandscape = 0f;

        private Vector2Int lastScreenSize;
        private Rect lastSafeArea;
        private bool cachedDesignSize;

        /*private const float defaultRatio = 2.05f;
        private const float defaultPortraitHeight = 1440f;*/

        /*private void OnEnable()
        {
            CacheDesignSize();
            Apply();
        }*/

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (panelRoot != null)
            {
                anchoredY = panelRoot.anchoredPosition.y;
            }
        }
#endif

        private IEnumerator Start()
        {
            // cho unity tinh toan height chinh xac cua canvas.
            yield return null;

            //CacheDesignSize();
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
            if (lastScreenSize.x != Screen.width ||
                lastScreenSize.y != Screen.height)
                return true;

            if (useSafeArea && lastSafeArea != Screen.safeArea)
                return true;

            return false;
        }

        private void CacheDesignSize()
        {
            if ( /*!autoDetectDesignSize ||*/
                cachedDesignSize ||
                panelRoot == null)
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
            if (size.x > 0 &&
                size.y > 0)
            {
                /*designSize = size;*/
                cachedDesignSize = true;
            }
        }

        [Button]
        private void Apply()
        {
            if (panelRoot == null)
                return;

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

            // ko su dung dynamic vi moi lan thay doi orientation, UI can thoi gian hien thi
            //float yOffset = Mathf.Max(0f, panelRoot.anchoredPosition.y);
            float yOffset = Mathf.Max(0f, changePosY ? yPosLandscape : anchoredY);
            float finalY = (panelRoot.root as RectTransform)!.rect.height;

            var sizeDelta = panelRoot.sizeDelta;
            float widthRatio = area.width / sizeDelta.x;
            float heightRatio = (finalY - yOffset) / sizeDelta.y;
            float scale = 1f;

            switch (fitMode)
            {
                case FitMode.FitInside:
                    // fit inside sua lai co logic la tu scale de phu hop voi man hinh hien tai.
                    heightRatio = (finalY - yOffset) / Screen.currentResolution.height;
                    scale = heightRatio;
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