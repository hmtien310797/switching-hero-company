using Cysharp.Threading.Tasks;
using DG.Tweening;
using EasyTextEffects;
using Immortal_Switch.Scripts.UI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Tutorial.Views
{
    public class TutorialData
    {
        public string LocalizeKey;
        public string ActionType;

        [CanBeNull]
        public RectTransform Target;
    }

    public class TutorialView : AnimatedUIView
    {
        [Header("View references")]
        [SerializeField]
        private RectTransform mask;

        [SerializeField]
        private Image overlay;

        [SerializeField]
        private Button btnSkip;

        [Header("Properties")]
        [SerializeField]
        private float cornerRadiusPx = 24f;

        [SerializeField]
        private float softnessPx = 8f;

        [SerializeField]
        private float padding = 20f;

        [Header("Finger references")]
        [SerializeField]
        private RectTransform finger;

        [SerializeField]
        private Vector2 fingerOffset = new(40f, -40f);

        [SerializeField]
        private float pullBackDistance = 24f;

        [SerializeField]
        private float duration = 0.35f;

        [SerializeField]
        private float startScale = 1f;

        [SerializeField]
        private float endScale = 1.2f;

        [Header("Story references")]
        [SerializeField]
        private RectTransform rtStory;

        [SerializeField]
        private TextMeshProUGUI txtStory;

        [SerializeField]
        private TextEffect txtEffectStory;

        [SerializeField]
        private float storySpacing = 24f;

        [SerializeField]
        private float screenPadding = 20f;

        [SerializeField]
        private Button btnStory;

        [SerializeField]
        private Button btnMask;

        // --- Private Fields ---
        private Sequence _fingerSequence;
        private RectTransform _rtCanvas;
        private RectTransform _rtBtnMask;
        private Canvas _canvas;
        private Material _material;
        private Vector2 _canvasSize;
        private Vector2 _orgStoryAnchoredPosition;

        private static readonly int CanvasSize = Shader.PropertyToID("__canvasSize");
        private static readonly int HoleCenter = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleSize = Shader.PropertyToID("_HoleSize");
        private static readonly int CornerRadius = Shader.PropertyToID("_CornerRadius");
        private static readonly int Softness = Shader.PropertyToID("_Softness");

        private void Awake()
        {
            TutorialManager.Instance.OnCompleteTutorial += OnCompleteTutorial;
            TutorialManager.Instance.OnChangeStep += OnChangeStep;

#if UNITY_EDITOR
            btnSkip.gameObject.SetActive(true);
            btnSkip.onClick.AddListener(OnClickSkip);
#else
            btnSkip.gameObject.SetActive(false);
#endif

            btnStory.onClick.AddListener(OnClickFocus);
            btnMask.onClick.AddListener(OnClickFocus);

            _canvas = mask.root.GetComponent<Canvas>();
            _rtCanvas = _canvas.transform as RectTransform;
            _rtBtnMask = btnMask.transform as RectTransform;
            _orgStoryAnchoredPosition = rtStory.anchoredPosition;

            _material = Instantiate(overlay.material);
            overlay.material = _material;

            CacheCanvas();
        }

        private void OnClickSkip()
        {
            TutorialManager.Instance.OnSkip();
        }

        private void OnChangeStep(TutorialData obj)
        {
            RefreshVisual(obj);
        }

        private void OnCompleteTutorial()
        {
            UIManager.Instance.Close<TutorialView>();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            btnSkip.onClick.RemoveListener(OnClickSkip);
#endif

            btnStory.onClick.RemoveListener(OnClickFocus);
            btnMask.onClick.RemoveListener(OnClickFocus);

            TutorialManager.Instance.OnCompleteTutorial -= OnCompleteTutorial;
            TutorialManager.Instance.OnChangeStep -= OnChangeStep;
        }

        private void OnClickFocus()
        {
            TutorialManager.Instance.FireOnClick().Forget();
        }

        private void CacheCanvas()
        {
            if (_rtCanvas != null)
            {
                _canvasSize = _rtCanvas.rect.size;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            CacheCanvas();
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is TutorialData tutorial)
            {
                RefreshVisual(tutorial);
            }
        }

        private void RefreshVisual(TutorialData tutorial)
        {
            if (!string.IsNullOrWhiteSpace(tutorial.LocalizeKey))
            {
                ShowStory(tutorial.LocalizeKey);
            }

            if (tutorial.Target != null)
            {
                btnStory.interactable = false;
                Focus(tutorial.Target);
                UpdateStoryPosition(_rtBtnMask);
            }
            else
            {
                btnMask.gameObject.SetActive(false);
                finger.gameObject.SetActive(false);
                _material.SetVector(HoleSize, Vector2.zero);
                _material.SetFloat(CornerRadius, 0);
                _material.SetFloat(Softness, 0);
                ResetOriginStoryAnchored();
            }
        }

        private void ResetOriginStoryAnchored()
        {
            rtStory.anchoredPosition = _orgStoryAnchoredPosition;
        }

        private void ShowStory(string txt)
        {
            rtStory.gameObject.SetActive(true);

            txtStory.text = txt;
            btnStory.interactable = true;

            txtEffectStory.StopOnStartEffects();
            txtEffectStory.Refresh();
        }

        public void Focus(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _rtCanvas,
                target
            );

            var center = (Vector2)bounds.center + _canvasSize * 0.5f;
            var size = (Vector2)bounds.size;
            size += Vector2.one * padding * 2f;

            var normalizedCenter = new Vector2(
                center.x / _canvasSize.x,
                center.y / _canvasSize.y
            );

            var normalizedSize = new Vector2(
                size.x / _canvasSize.x,
                size.y / _canvasSize.y
            );

            var safeRadius = Mathf.Min(
                cornerRadiusPx,
                size.x * 0.5f,
                size.y * 0.5f
            );

            _material.SetVector(CanvasSize, _canvasSize);
            _material.SetVector(HoleCenter, normalizedCenter);
            _material.SetVector(HoleSize, normalizedSize);
            _material.SetFloat(CornerRadius, safeRadius);
            _material.SetFloat(Softness, softnessPx);

            var worldCenter = target.TransformPoint(target.rect.center);

            btnMask.gameObject.SetActive(true);
            finger.gameObject.SetActive(true);

            SetMaskSize(worldCenter, size);
            MoveFingerToTarget(worldCenter);
        }

        private void SetMaskSize(Vector3 worldCenter, Vector2 size)
        {
            _rtBtnMask.position = worldCenter;

            _rtBtnMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _rtBtnMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private void MoveFingerToTarget(Vector3 worldCenter)
        {
            finger.position = worldCenter;
            finger.anchoredPosition += fingerOffset;

            PlayFingerAnimation();
        }

        private void UpdateStoryPosition(RectTransform child)
        {
            var canvasRect = _rtCanvas.rect;

            var maskBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _rtCanvas,
                child);

            var storyHeight = rtStory.rect.height;

            // Vì anchor ở Bottom nên đây là offset từ đáy canvas
            var yAbove = maskBounds.max.y + canvasRect.height * 0.5f + storySpacing;
            var yBelow = maskBounds.min.y + canvasRect.height * 0.5f - storyHeight - storySpacing;
            var canPlaceAbove = yAbove + storyHeight <= canvasRect.height - screenPadding;
            var canPlaceBelow = yBelow >= screenPadding;
            var pos = rtStory.anchoredPosition;

            if (canPlaceAbove)
            {
                pos.y = yAbove;
            }
            else if (canPlaceBelow)
            {
                pos.y = yBelow;
            }
            else
            {
                // Không đủ chỗ cả trên lẫn dưới thì ưu tiên phía có nhiều diện tích hơn
                var spaceAbove = canvasRect.height - (maskBounds.max.y + canvasRect.height * 0.5f);
                var spaceBelow = maskBounds.min.y + canvasRect.height * 0.5f;

                if (spaceAbove >= spaceBelow)
                {
                    pos.y = Mathf.Min(yAbove, canvasRect.height - storyHeight - screenPadding);
                }
                else
                {
                    pos.y = Mathf.Max(yBelow, screenPadding);
                }
            }

            rtStory.anchoredPosition = pos;
        }

        private void PlayFingerAnimation()
        {
            _fingerSequence?.Kill();

            // vi tri bat dau
            var targetPos = finger.anchoredPosition;

            // hướng từ finger -> target
            var direction = fingerOffset.normalized;

            // vị trí giật lùi
            var backPos = targetPos + direction * pullBackDistance;
            finger.anchoredPosition = targetPos;

            _fingerSequence = DOTween.Sequence()
                .Join(
                    finger.DOAnchorPos(backPos, duration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                )
                .Join(
                    finger.DOScale(startScale, duration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                );
        }
    }
}