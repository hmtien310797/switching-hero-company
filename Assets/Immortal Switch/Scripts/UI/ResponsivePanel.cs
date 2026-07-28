using System.Collections.Generic;
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

        [Required]
        [SerializeField]
        private RectTransform panelRoot;

        [Header("Mode")]
        [SerializeField]
        private FitMode fitMode = FitMode.FitInside;

        [SerializeField]
        private bool useSafeArea = true;

        [Header("Clamp")]
        [SerializeField]
        private float minScale = 0.35f;

        [SerializeField]
        private float maxScale = 1f;

        [Header("Anchored position for some case")]
        [SerializeField]
        private float offsetAnchoredY;

        [Header("Optional Y Offset")]
        [SerializeField]
        private bool changePosY = false;

        [Header("Optional Scale Reduction")]
        [SerializeField]
        private bool useScaleReduction = false;

        [SerializeField]
        [Range(0f, 100f)]
        private float scaleReductionPercent = 0f;

        [Header("References child")]
        [SerializeField]
        private List<RectTransform> children = new();

        private Vector2Int lastScreenSize;
        private Rect lastSafeArea;

        private bool cachedDesignSize;

        //for demo
        public bool useConstScale;
        public float constScale;

        /*private const float defaultRatio = 2.05f;
        private const float defaultPortraitHeight = 1440f;*/

        /*private void OnEnable()
        {
            CacheDesignSize();
            Apply();
        }*/

        private void Start()
        {
            Apply();
        }

        private void LateUpdate()
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

        [Button]
        private void Apply()
        {
            if (panelRoot == null)
                return;

            // yeu cau update canvas truoc khi apply
            Canvas.ForceUpdateCanvases();

            /*bool isPortrait = Screen.height >= Screen.width;*/

            float scale = CalculateScale();
            var lastScale = Vector3.one * scale;

            panelRoot.localScale = lastScale;

            if (changePosY)
            {
                Vector2 pos = panelRoot.anchoredPosition;
                pos.y = TopMainView.Instance.BottomAnchorY + offsetAnchoredY;
                panelRoot.anchoredPosition = pos;
            }

            /*if (changePosY)
            {
                Vector2 pos = panelRoot.anchoredPosition;
                pos.y = isPortrait ? yPosPortrait : yPosLandscape;
                panelRoot.anchoredPosition = pos;
            }*/

            if (children.Count > 0)
            {
                foreach (var child in children)
                {
                    child.localScale = lastScale;
                }
            }

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
        }

        private float CalculateScale()
        {
            bool isPortrait = Screen.height >= Screen.width;

            if (isPortrait)
                return panelRoot.localScale.x;

            if (useConstScale)
                return constScale;

            Rect area = useSafeArea
                ? Screen.safeArea
                : new Rect(0, 0, Screen.width, Screen.height);

            // ko su dung dynamic vi moi lan thay doi orientation, UI can thoi gian hien thi
            //float yOffset = Mathf.Max(0f, panelRoot.anchoredPosition.y);
            float yOffset = TopMainView.Instance.BottomAnchorY + offsetAnchoredY;
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

            if (useScaleReduction && !isPortrait)
            {
                scale *= 1f - scaleReductionPercent / 100f;
            }

            return Mathf.Clamp(scale, minScale, maxScale);
        }
    }
}