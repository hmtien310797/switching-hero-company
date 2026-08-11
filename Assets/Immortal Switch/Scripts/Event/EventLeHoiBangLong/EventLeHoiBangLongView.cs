using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    /// <summary>Chế độ framing của mascot vào RawImage.</summary>
    public enum MascotFitMode
    {
        /// <summary>Fill kín cả 2 trục, crop phần tràn (không viền).</summary>
        Cover,
        /// <summary>Fit vừa trong khung, để viền (không crop).</summary>
        Contain,
    }

    public class EventLeHoiBangLongView : AnimatedUIView
    {
        [SerializeField]
        private UIEventLeHoiBangLongLayoutController layoutHorizontal;

        [SerializeField]
        private UIEventLeHoiBangLongLayoutController layoutVertical;

        [SerializeField] 
        private Transform verticalLayoutTransform;
        
        [SerializeField] 
        private Transform horizontalLayoutTransform;
        
        [SerializeField] 
        private Vector2 verticalLayoutSize;
        
        [SerializeField] 
        private Vector2 horizontalLayoutSize;

        [SerializeField]
        private EventLeHoiBangLongTopLayout topLayout;

        [Header("Mascot Spine (Option 2: SkeletonAnimation world + Camera → RenderTexture → RawImage)")]
        [Tooltip("Asset spine của mascot (vd ice_dragon_background_SkeletonData).")]
        [SerializeField]
        private SkeletonDataAsset mascotSkeletonDataAsset;

        [Tooltip("Animation chạy ở hướng ngang. Để trống nếu chỉ muốn setup pose.")]
        [SerializeField]
        private string mascotLandscapeAnimation;

        [Tooltip("RawImage nằm trong horizontal_layout. Code sẽ gán RenderTexture.")]
        [SerializeField]
        private RawImage mascotRawImageHorizontal;

        [Tooltip("Cạnh dài của RenderTexture (pixel). Cao hơn = nét hơn, tốn GPU hơn. Nền 1024+ cho background full-screen.")]
        [SerializeField]
        private int mascotRenderTextureSize = 1024;

        [Tooltip("Cover: =1 fill kín; <1 crop thêm; >1 thu nhỏ lại (để viền).")]
        [SerializeField]
        private float mascotFramePadding = 1.0f;

        [Tooltip("Cover = fill kín khung (crop tràn); Contain = fit (có viền).")]
        [SerializeField]
        private MascotFitMode mascotFitMode = MascotFitMode.Cover;

        [Tooltip("Layer dành riêng cho spine (camera phụ chỉ render layer này). Slot 12 = SpineRT.")]
        [SerializeField]
        private int mascotLayer = 12;

        // --- Private Fields ---

        private bool _mascotSetup;
        private GameObject _spineGo;
        private SkeletonAnimation _skeletonAnim;
        private Camera _mascotCam;
        private RenderTexture _rt;
        private Material _rawMat;
        private Bounds _mascotBounds;
        private float _rtAspect = -1f;

        private void Awake()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;

            layoutHorizontal.DisableLayouts();
            layoutVertical.DisableLayouts();

            topLayout.Bind(ChangeLayout, OnClose, OnHelp);
            layoutHorizontal.Bind(ChangeLayout);
            layoutVertical.Bind(ChangeLayout);
        }

        private void OnHelp()
        {
            UIManager.Instance
                .TogglePopupAsync<PopupEventInfoView>(new PopupEventInfoArgs
                {
                    DescKey = "desc_key",
                    TitleKey = "title_key",
                })
                .Forget();
        }

        private void OnClose()
        {
            UIManager.Instance.TogglePopupAsync<EventLeHoiBangLongView>().Forget();
        }

        private void OnEnable()
        {
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
            RefreshAndBindAsync().Forget();
        }

        /// <summary>Tải state server (7 ngày check-in, nhiệm vụ, milestone, tỉ lệ gacha) mỗi lần
        /// view được mở trước khi bind layout — tránh hiện UI rỗng rồi mới cập nhật, cùng cách
        /// EventWheelView.RefreshAndBindAsync làm.</summary>
        private async UniTaskVoid RefreshAndBindAsync()
        {
            await EventLeHoiBangLongManager.Instance.RefreshAsync();
            ChangeLayout(EEventLeHoiBangLongLayoutType.Main);
        }

        private void OnDestroy()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
            TeardownMascot();
        }

        private void ChangeLayout(EEventLeHoiBangLongLayoutType type)
        {
            topLayout.SetEnableBack(type != EEventLeHoiBangLongLayoutType.Main);
            layoutHorizontal.ChangeLayout(type);
            layoutVertical.ChangeLayout(type);
        }

        // AnimatedUIView.PlayShowAsync chạy OnShow trước khi kích hoạt view root
        // (EnsureInit SetActive(false) root ở dòng 45, OnShow ở dòng 56, active lại ở dòng 63),
        // nên setup + play mascot phải đặt SAU khi base xong — view root đã active.
        // (Awake đã đăng ký OnOrientationChanged — KHÔNG đăng ký lại ở đây tránh stack trùng.)
        public override async UniTask PlayShowAsync(object args)
        {
            EnsureMascot();
            RefreshMascotFraming(ScreenOrientationTracker.Instance.CurrentMode);
            PlayMascotAnimation();
            await base.PlayShowAsync(args);
            GameCameraController.Instance.TriggerHeroCamera(false);
        }

        public override async UniTask PlayHideAsync()
        {
            // Dừng + ẩn spine/camera khi đóng để tiết kiệm CPU (không render RT khi view ẩn).
            StopMascot();
            GameCameraController.Instance.TriggerHeroCamera(true);
            await base.PlayHideAsync();
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    layoutHorizontal.gameObject.SetActive(false);
                    layoutVertical.gameObject.SetActive(true);
                    topLayout.SetEnableHelp(false);
                    RefreshMascotFraming(obj); 
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    layoutHorizontal.gameObject.SetActive(true);
                    layoutVertical.gameObject.SetActive(false);
                    topLayout.SetEnableHelp(true);
                    RefreshMascotFraming(obj); 
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }

        // ---- Mascot (Option 2) ----
        // Spine render world-space vào RenderTexture qua camera phụ (layer riêng), RawImage hiện RT.
        // Hoàn toàn tách khỏi Canvas: không rebuild, không re-batch, z-order do RawImage (UI element).
        // Spine + camera tạo bằng code ở (0,-10000,0), xa frustum Camera.main → không hiện trên màn hình chính.
        private void EnsureMascot()
        {
            if (_mascotSetup) return;

            if (mascotSkeletonDataAsset == null)
            {
                Debug.LogWarning("[EventLeHoiBangLongView] mascotSkeletonDataAsset chưa gán — mascot sẽ không render.", this);
                return;
            }

            int layer = mascotLayer;

            // Spine GameObject: root, xa UI, layer riêng.
            _spineGo = new GameObject("__MascotSpine") { layer = layer };
            _spineGo.transform.position = new Vector3(0f, -10000f, 0f);
            _skeletonAnim = _spineGo.AddComponent<SkeletonAnimation>();
            _skeletonAnim.skeletonDataAsset = mascotSkeletonDataAsset;
            _skeletonAnim.Initialize(true);
            _skeletonAnim.Skeleton.SetToSetupPose();
            _skeletonAnim.LateUpdate(); // build mesh để lấy bounds

            // Camera phụ: chỉ render layer `mascotLayer`, nền trong suốt.
            var camGo = new GameObject("__MascotCamera");
            _mascotCam = camGo.AddComponent<Camera>();
            _mascotCam.orthographic = true;
            _mascotCam.cullingMask = 1 << layer;
            _mascotCam.clearFlags = CameraClearFlags.SolidColor;
            _mascotCam.backgroundColor = Color.clear;
            _mascotCam.nearClipPlane = 0.1f;
            _mascotCam.farClipPlane = 1000f;
            _mascotCam.depth = -100f;
            _mascotCam.enabled = false; // bật trong RefreshMascotFraming sau khi có targetTexture (tránh render ra màn hình)

            camGo.transform.SetParent(_spineGo.transform, true);

            // Material premultiplied cho RawImage (RT chứa spine premultiplied → tránh dark halo).
            _rawMat = new Material(Shader.Find("Spine/SkeletonGraphic"));
            _rawMat.EnableKeyword("_CANVAS_GROUP_COMPATIBLE");

            // Lưu bounds setup-pose (world) để framing ổn định, không jitter theo animation.
            var mr = _skeletonAnim.GetComponent<MeshRenderer>();
            if (mr != null && mr.bounds.size.x > 0f && mr.bounds.size.y > 0f)
            {
                _mascotBounds = mr.bounds;
            }
            else
            {
                var sd = _skeletonAnim.skeletonDataAsset.GetSkeletonData(false);
                Vector3 c = new Vector3(sd.X + sd.Width * 0.5f, sd.Y + sd.Height * 0.5f, 0f) + _spineGo.transform.position;
                _mascotBounds = new Bounds(c, new Vector3(sd.Width, sd.Height, 0f));
            }

            DontDestroyOnLoad(_spineGo); // sống qua scene transition như UIManager root
            _mascotSetup = true;
        }

        // RT aspect khớp RawImage active; Cover/Contain framing. Gọi mỗi lần show + khi xoay orientation.
        private void RefreshMascotFraming(ScreenOrientationTracker.ScreenViewMode screenViewMode)
        {
            if (!_mascotSetup || _skeletonAnim == null || _mascotCam == null) return;
            
            if (mascotRawImageHorizontal == null) return;
            switch (screenViewMode)
            {
                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    mascotRawImageHorizontal.transform.SetParent(horizontalLayoutTransform);
                    mascotRawImageHorizontal.rectTransform.sizeDelta = horizontalLayoutSize;
                    break;
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    mascotRawImageHorizontal.transform.SetParent(verticalLayoutTransform);
                    mascotRawImageHorizontal.rectTransform.sizeDelta = verticalLayoutSize;
                    break;
            }
            mascotRawImageHorizontal.transform.SetSiblingIndex(1);

            Rect r = mascotRawImageHorizontal.rectTransform.rect;
            float aspect = (r.height > 0f) ? r.width / r.height : 1f;
            if (aspect <= 0f || float.IsNaN(aspect)) aspect = 1f;

            // (Re)tạo RT khi aspect đổi (landscape 16:9 ↔ portrait 9:16).
            if (_rt == null || Mathf.Abs(aspect - _rtAspect) > 0.01f)
            {
                int longer = Mathf.Max(16, mascotRenderTextureSize);
                int rtW, rtH;
                if (aspect >= 1f)
                {
                    rtW = longer;
                    rtH = Mathf.Max(16, Mathf.RoundToInt(longer / aspect));
                }
                else
                {
                    rtH = longer;
                    rtW = Mathf.Max(16, Mathf.RoundToInt(longer * aspect));
                }

                if (_rt != null)
                {
                    _rt.Release();
                    Destroy(_rt);
                }
                _rt = new RenderTexture(rtW, rtH, 0, RenderTextureFormat.ARGB32)
                {
                    name = "MascotRT",
                    filterMode = FilterMode.Bilinear,
                };
                _rt.Create();
                _rtAspect = aspect;

                AssignRawImage(mascotRawImageHorizontal);
                _mascotCam.targetTexture = _rt;
                _mascotCam.aspect = aspect;
            }

            // Framing theo aspect RT (ortho = nửa chiều cao nhìn thấy).
            float W = _mascotBounds.size.x;
            float H = _mascotBounds.size.y;
            float ortho;
            if (mascotFitMode == MascotFitMode.Cover)
                ortho = Mathf.Min(H * 0.5f, W * 0.5f / aspect); // fill cả 2 trục, crop phần tràn
            else
                ortho = Mathf.Max(H * 0.5f, W * 0.5f / aspect); // fit trong khung, để viền
            //_mascotCam.orthographicSize = Mathf.Max(ortho * mascotFramePadding, 0.1f);
            _mascotCam.orthographicSize = 5.5f;
            
            Vector3 center = _mascotBounds.center;
            _mascotCam.transform.position = center + new Vector3(0f, 0f, -10f);
            _mascotCam.transform.LookAt(center, Vector3.up);
            _mascotCam.enabled = true;
        }

        private void AssignRawImage(RawImage rawImage)
        {
            if (rawImage == null || _rt == null) return;
            rawImage.texture = _rt;
            rawImage.material = _rawMat;
        }

        private void PlayMascotAnimation()
        {
            if (_skeletonAnim == null) return;

            _spineGo.SetActive(true);
            _skeletonAnim.AnimationState.ClearTracks();
            _skeletonAnim.Skeleton.SetToSetupPose();
            SetMascotAnimation(mascotLandscapeAnimation);
        }

        // FindAnimation null-safe → không ném exception khi đổi asset có tên animation khác/không có.
        private void SetMascotAnimation(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return;
            var anim = _skeletonAnim.skeletonDataAsset.GetSkeletonData(false).FindAnimation(animName);
            if (anim != null)
                _skeletonAnim.AnimationState.SetAnimation(0, anim, true);
        }

        private void StopMascot()
        {
            if (_skeletonAnim != null && _skeletonAnim.AnimationState != null)
                _skeletonAnim.AnimationState.ClearTracks();

            if (_spineGo != null)
                _spineGo.SetActive(false);
        }

        private void TeardownMascot()
        {
            if (_spineGo != null)
            {
                Destroy(_spineGo); // huỷ cả camera con
                _spineGo = null;
            }
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }
            if (_rawMat != null)
            {
                Destroy(_rawMat);
                _rawMat = null;
            }
            _skeletonAnim = null;
            _mascotCam = null;
            _mascotSetup = false;
            _rtAspect = -1f;
        }
    }
}
