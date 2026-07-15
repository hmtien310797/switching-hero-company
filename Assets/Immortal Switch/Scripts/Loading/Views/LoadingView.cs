using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Loading.Views
{
    public class LoadingView : UIView
    {
        [Header("Hiện ngay và chặn toàn bộ thao tác")]
        [SerializeField]
        private CanvasGroup blockerCanvasGroup;

        [Header("Spinner và nội dung loading")]
        [SerializeField]
        private CanvasGroup contentCanvasGroup;

        [SerializeField, Min(0f)]
        private float contentDelay = 1.5f;

        private CancellationTokenSource showContentCts;

        public override void OnShow(object args)
        {
            base.OnShow(args);

            bool showImmediately = args is bool value && value;

            ShowAsync(showImmediately).Forget();
        }

        public override void OnHide()
        {
            CancelDelayedContent();
            HideImmediately();

            base.OnHide();
        }

        /// <summary>
        /// Dùng khi LoadingView đang mở ở chế độ delay,
        /// nhưng có request khác yêu cầu hiện nội dung ngay.
        /// </summary>
        public void ShowContentImmediately()
        {
            CancelDelayedContent();

            SetCanvasGroup(
                contentCanvasGroup,
                alpha: 1f,
                interactable: false,
                blocksRaycasts: false);
        }

        private async UniTaskVoid ShowAsync(bool showImmediately)
        {
            CancelDelayedContent();

            // Nền tối và raycast blocker luôn hiện ngay.
            SetCanvasGroup(
                blockerCanvasGroup,
                alpha: 1f,
                interactable: true,
                blocksRaycasts: true);

            if (showImmediately)
            {
                ShowContentImmediately();
                return;
            }

            // Chưa hiện spinner và text.
            SetCanvasGroup(
                contentCanvasGroup,
                alpha: 0f,
                interactable: false,
                blocksRaycasts: false);

            showContentCts = new CancellationTokenSource();
            CancellationToken cancellationToken = showContentCts.Token;

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(contentDelay),
                    cancellationToken: cancellationToken);

                SetCanvasGroup(
                    contentCanvasGroup,
                    alpha: 1f,
                    interactable: false,
                    blocksRaycasts: false);
            }
            catch (OperationCanceledException)
            {
                // Loading đã đóng hoặc được chuyển sang hiện ngay.
            }
        }

        private void HideImmediately()
        {
            SetCanvasGroup(
                blockerCanvasGroup,
                alpha: 0f,
                interactable: false,
                blocksRaycasts: false);

            SetCanvasGroup(
                contentCanvasGroup,
                alpha: 0f,
                interactable: false,
                blocksRaycasts: false);
        }

        private void CancelDelayedContent()
        {
            if (showContentCts == null)
            {
                return;
            }

            showContentCts.Cancel();
            showContentCts.Dispose();
            showContentCts = null;
        }

        private static void SetCanvasGroup(
            CanvasGroup canvasGroup,
            float alpha,
            bool interactable,
            bool blocksRaycasts)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = alpha;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = blocksRaycasts;
        }

        private void OnDestroy()
        {
            CancelDelayedContent();
        }
    }
    
    public static class LoadingService
    {
        private static LoadingView currentView;

        private static int requestCount;
        private static bool isOpening;
        private static bool immediateRequested;
        
        public static void Show(bool immediately = false)
        {
            requestCount++;

            if (immediately)
            {
                immediateRequested = true;
            }

            ShowInternalAsync().Forget();
        }
        
        public static void Hide()
        {
            requestCount = Mathf.Max(0, requestCount - 1);

            if (requestCount > 0)
            {
                return;
            }

            immediateRequested = false;

            if (UIManager.Instance == null)
            {
                currentView = null;
                return;
            }

            if (UIManager.Instance.IsOpen<LoadingView>())
            {
                UIManager.Instance.Close<LoadingView>();
            }

            currentView = null;
        }
        
        public static void ForceHide()
        {
            requestCount = 0;
            immediateRequested = false;

            if (UIManager.Instance != null &&
                UIManager.Instance.IsOpen<LoadingView>())
            {
                UIManager.Instance.Close<LoadingView>();
            }

            currentView = null;
            isOpening = false;
        }

        public static bool IsShowing =>
            requestCount > 0;

        private static async UniTaskVoid ShowInternalAsync()
        {
            if (currentView != null)
            {
                if (immediateRequested)
                {
                    currentView.ShowContentImmediately();
                }

                return;
            }
            
            if (isOpening)
            {
                return;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError("[Loading] UIManager.Instance is null.");
                return;
            }

            isOpening = true;

            try
            {
                bool showImmediately = immediateRequested;

                currentView =
                    await UIManager.Instance.OpenPopupAsync<LoadingView>(
                        args: showImmediately,
                        withBackdrop: false);
                
                if (requestCount <= 0)
                {
                    if (UIManager.Instance.IsOpen<LoadingView>())
                    {
                        UIManager.Instance.Close<LoadingView>();
                    }

                    currentView = null;
                    return;
                }
                
                if (immediateRequested && currentView != null)
                {
                    currentView.ShowContentImmediately();
                }
            }
            finally
            {
                isOpening = false;
            }
        }
    }
}