using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Bag.Views.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Views
{
    /// <summary>
    /// Thông tin dùng để hiển thị popup reward.
    /// </summary>
    public class PopupRewardArgs
    {
        /// <summary>
        /// ds item thuong
        /// </summary>
        public List<ItemRewardData> Rewards { get; set; }

        /// <summary>
        /// callback khi close popup.
        /// </summary>
        public Action OnClose { get; set; }
    }

    public class PopupRewardView : AnimatedUIView
    {
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIBagItem rewardPrefab;

        // --- Private Fields ---
        private List<UIBagItem> _rewards = new();
        private PopupRewardArgs _args;

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is PopupRewardArgs runtime)
            {
                _args = runtime;
                RefreshRewards(runtime.Rewards);
            }
        }

        public override void OnHide()
        {
            base.OnHide();
            _args?.OnClose?.Invoke();
        }

        private void RefreshRewards(List<ItemRewardData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];

                if (_rewards.Count > i)
                {
                    var clone = _rewards[i];
                    clone.gameObject.SetActive(true);

                    clone.Bind(reward.ItemIcon,
                        reward.TierInfo.border,
                        reward.TierInfo.background,
                        reward.TierInfo.tierIcon,
                        reward.Quantity
                    );
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);

                    clone.Bind(reward.ItemIcon,
                        reward.TierInfo.border,
                        reward.TierInfo.background,
                        reward.TierInfo.tierIcon,
                        reward.Quantity
                    );

                    _rewards.Add(clone);
                }
            }

            for (int i = rewards.Count; i < _rewards.Count; i++)
            {
                _rewards[i].gameObject.SetActive(false);
            }
        }
    }
}