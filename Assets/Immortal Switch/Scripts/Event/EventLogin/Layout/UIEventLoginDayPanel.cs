using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLogin.Layout
{
    public class UIEventLoginDayPanel : MonoBehaviour
    {
        [Header("Button references")]
        [SerializeField]
        private Button btnPrev;

        [SerializeField]
        private Button btnNext;

        [Header("Tab references")]
        [SerializeField]
        private RectTransform tabContainer;

        [SerializeField]
        private UITabPreset tabPrefab;

        [SerializeField]
        private ScrollRect tabScroll;

        // --- Private Fields ---
        private SimpleUIPool<UITabPreset> _pools;
        private UITabPreset _selectedTab;
        private Action<int> _onChangeDay;

        private int _scrollRequestId;
        private int _totalDay;
        private int _currentDay;
        private int _selectedDay;

        private void Awake()
        {
            btnPrev.onClick.AddListener(OnClickPrev);
            btnNext.onClick.AddListener(OnClickNext);
        }

        private void OnClickNext()
        {
            OnTabChange(1);
        }

        private void OnClickPrev()
        {
            OnTabChange(-1);
        }

        private void OnTabChange(int direction)
        {
            var idx = Math.Min(Math.Max(_selectedDay + direction - 1, 0), _totalDay - 1);

            OnClickDay(idx);
            MoveScrollToSelectedTabAsync().Forget();
        }

        private void RefreshButton()
        {
            if (_selectedDay == _currentDay)
            {
                btnNext.interactable = false;
                btnPrev.interactable = true;
            }
            else if (_selectedDay <= 1)
            {
                btnNext.interactable = true;
                btnPrev.interactable = false;
            }
            else if (_selectedDay < _currentDay)
            {
                btnNext.interactable = true;
                btnPrev.interactable = true;
            }
        }

        public void Bind(
            int totalDay,
            int currentDay,
            Action<int> onChangeDay
        )
        {
            _onChangeDay = onChangeDay;
            _totalDay = totalDay;
            _currentDay = currentDay;
            _selectedDay = currentDay;

            RefreshDays(totalDay, currentDay);
            OnClickDay(currentDay - 1);
            MoveScrollToSelectedTabAsync(true).Forget();
        }

        private void OnDisable()
        {
            _scrollRequestId++;
        }

        private void RefreshDays(int totalDay, int currentDay)
        {
            _pools ??= new SimpleUIPool<UITabPreset>(tabPrefab, tabContainer);

            for (int i = 0; i < totalDay; i++)
            {
                var day = i + 1;
                var clone = _pools.Get(i);

                clone.Bind(i, $"Ngày {day}", OnClickDay);

                if (day > currentDay)
                {
                    clone.SetStatus(ETabPresetStatus.Lock);
                }
                else if (day == currentDay)
                {
                    clone.SetStatus(ETabPresetStatus.Selected);
                }
                else
                {
                    clone.SetStatus(ETabPresetStatus.Normal);
                }
            }

            _pools.ReleaseFrom(totalDay);
        }

        private UITabPreset GetTabByDay(int day)
        {
            return _pools.Get(day - 1);
        }

        private void OnClickDay(int idx)
        {
            if (_selectedTab != null)
            {
                _selectedTab.SetStatus(ETabPresetStatus.Normal);
                _selectedTab = null;
            }

            var day = idx + 1;

            _selectedDay = day;
            _selectedTab = GetTabByDay(day);

            RefreshButton();
            _selectedTab.SetStatus(ETabPresetStatus.Selected);
            _onChangeDay?.Invoke(day);
        }

        private async UniTaskVoid MoveScrollToSelectedTabAsync(bool forceRebuild = false)
        {
            var requestId = ++_scrollRequestId;

            // ContentSizeFitter và HorizontalLayoutGroup chỉ có kích thước chính xác
            // sau khi Canvas hoàn tất layout của frame hiện tại.
            await UniTask.NextFrame();

            if (this == null ||
                requestId != _scrollRequestId ||
                !isActiveAndEnabled)
            {
                return;
            }

            if (tabScroll == null ||
                _selectedTab == null)
            {
                return;
            }

            if (forceRebuild)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(tabContainer);
                Canvas.ForceUpdateCanvases();
            }

            var viewport = tabScroll.viewport;
            var selectedRect = _selectedTab.GetComponent<RectTransform>();

            var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, tabContainer);
            var selectedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selectedRect);
            var scrollableWidth = contentBounds.size.x - viewport.rect.width;

            tabScroll.StopMovement();

            if (scrollableWidth <= 0f)
            {
                tabScroll.horizontalNormalizedPosition = 0f;
                return;
            }

            var selectedCenterFromContentLeft = selectedBounds.center.x - contentBounds.min.x;
            var targetOffset = selectedCenterFromContentLeft - viewport.rect.width * 0.5f;

            tabScroll.horizontalNormalizedPosition = Mathf.Clamp01(targetOffset / scrollableWidth);
        }
    }
}