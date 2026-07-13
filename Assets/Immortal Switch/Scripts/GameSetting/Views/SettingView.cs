using System.Collections.Generic;
using Immortal_Switch.Scripts.GameSetting.Views.Layouts;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.GameSetting.Views
{
    public class SettingView : AnimatedUIView
    {
        [Header("Segment control")]
        [SerializeField]
        private List<GameObject> layouts = new();

        [SerializeField]
        private SegmentedControlStatic segmentControl;

        [SerializeField]
        private SegmentedControlOption mainSegment;

        [SerializeField]
        private SegmentedControlOption accountSegment;

        [SerializeField]
        private SegmentedControlOption otherSegment;

        [Header("Layout scripts")]
        [SerializeField]
        private UISettingMainLayout mainLayout;

        // --- Private Fields ---
        private int _currentIdx;

        private void Awake()
        {
            mainSegment.Bind(() => OnSegmentChanged(0));
            accountSegment.Bind(() => OnSegmentChanged(1));
            otherSegment.Bind(() => OnSegmentChanged(2));
        }

        private void OnEnable()
        {
            // main layout
            mainLayout.Bind(false, false, OnGgLink, OnLinkClaim);
            OnSegmentChanged(0);
        }

        private void OnLinkClaim()
        {
            Debug.Log("OnLinkClaim");
        }

        private void OnGgLink()
        {
            Debug.Log("OnGgLink");
        }

        private void OnSegmentChanged(int idx)
        {
            if (_currentIdx != 0)
            {
                RefreshLayout(-1);
            }

            if (layouts.Count > idx)
            {
                _currentIdx = idx;

                RefreshLayout(idx);
                segmentControl.SetSelected(idx);
            }
        }

        private void RefreshLayout(int idx)
        {
            for (int i = 0; i < layouts.Count; i++)
            {
                layouts[i].SetActive(i == idx);
            }
        }
    }
}