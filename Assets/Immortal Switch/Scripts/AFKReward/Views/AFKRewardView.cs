using System;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.AFKReward.Views
{
    public class AFKRewardArgs
    {
        /// <summary>
        /// claim thuong.
        /// value 1: nhan x2 hay ko
        /// </summary>
        public Action<bool> OnClaim { get; set; }
    }

    public class AFKRewardView : AnimatedUIView
    {
        [Header("View references")]
        [SerializeField]
        private TextMeshProUGUI txtAfkTotalTime;

        [SerializeField]
        private TextMeshProUGUI txtAfkCurrentTime;

        [SerializeField]
        private TextMeshProUGUI txtKilledMonsterCount;

        [SerializeField]
        private TextMeshProUGUI txtKillMonsterPerSecond;

        [SerializeField]
        private TextMeshProUGUI txtAds;

        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private Button btnClaimX2;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIReward rewardPrefab;

        // --- Private Fields ---
        private AFKRewardArgs _args;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
            btnClaimX2.onClick.AddListener(OnClickClaimX2);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
            btnClaimX2.onClick.RemoveListener(OnClickClaimX2);
        }

        private void OnClickClaim()
        {
            _args?.OnClaim?.Invoke(false);
        }

        private void OnClickClaimX2()
        {
            _args?.OnClaim?.Invoke(true);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is AFKRewardArgs runtime)
            {
                _args = runtime;
            }
        }
    }
}