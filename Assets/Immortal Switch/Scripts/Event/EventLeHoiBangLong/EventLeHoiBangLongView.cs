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

        [Tooltip("RawImage nằm trong horizontal_layout. Code sẽ gán RenderTexture.")]
        [SerializeField]
        private Transform spineAnimCanvas;
        
        [SerializeField]
        private SkeletonGraphic _skeletonAnim;

        // --- Private Fields ---

        private bool _mascotSetup;
        private GameObject _spineGo;
        private Camera _mascotCam;
        private RenderTexture _rt;
        private Material _rawMat;
        private Bounds _mascotBounds;
        private float _rtAspect = -1f;

        private void Awake()
        {
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
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
            RefreshAndBindAsync().Forget();
        }

        private void OnDisable()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        /// <summary>Tải state server (7 ngày check-in, nhiệm vụ, milestone, tỉ lệ gacha) mỗi lần
        /// view được mở trước khi bind layout — tránh hiện UI rỗng rồi mới cập nhật, cùng cách
        /// EventWheelView.RefreshAndBindAsync làm.</summary>
        private async UniTaskVoid RefreshAndBindAsync()
        {
            await EventLeHoiBangLongManager.Instance.RefreshAsync();
            ChangeLayout(EEventLeHoiBangLongLayoutType.Main);
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
            _skeletonAnim.AnimationState.ClearTracks();
            _skeletonAnim.Skeleton.SetToSetupPose();
            _skeletonAnim.AnimationState.SetAnimation(0, "landscape", true);
            RefreshMascotFraming(ScreenOrientationTracker.Instance.CurrentMode);
            await base.PlayShowAsync(args);
            GameCameraController.Instance.TriggerHeroCamera(false);
        }

        public override async UniTask PlayHideAsync()
        {
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

        // RT aspect khớp RawImage active; Cover/Contain framing. Gọi mỗi lần show + khi xoay orientation.
        private void RefreshMascotFraming(ScreenOrientationTracker.ScreenViewMode screenViewMode)
        {
            switch (screenViewMode)
            {
                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    spineAnimCanvas.SetParent(horizontalLayoutTransform);
                    spineAnimCanvas.SetAsFirstSibling();
                    _skeletonAnim.rectTransform.localScale = Vector3.one;
                    break;
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    spineAnimCanvas.SetParent(verticalLayoutTransform);
                    spineAnimCanvas.SetAsFirstSibling();
                    _skeletonAnim.rectTransform.localScale = Vector3.one * 1.7f;
                    break;
            }
        }
    }
}
