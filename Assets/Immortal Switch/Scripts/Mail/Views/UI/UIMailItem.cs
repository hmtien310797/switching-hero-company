using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Mail.Views.UI
{
    public class UIMailItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtDescription;

        [Header("Reward references")]
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIRewardQuantity rewardPrefab;

        [SerializeField]
        [Range(0f, float.MaxValue)]
        private float rewardScale = 0.75f;

        // --- Private Fields ---
        private SimpleUIPool<UIRewardQuantity> _pool;
        private Action<string> _onClickClaim;

        private string _mailId;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
        }

        private void OnClickClaim()
        {
            _onClickClaim?.Invoke(_mailId);
        }

        public void Bind(string mailId, string title, string description, List<ItemData> rewards, bool claimed,
            Action<string> onClickClaim)
        {
            _mailId = mailId;
            _onClickClaim = onClickClaim;

            txtTitle.text = title;
            txtDescription.text = description;

            var hasReward = rewards.Count > 0;

            btnClaim.gameObject.SetActive(hasReward && !claimed);

            btnClaim.interactable = hasReward && !claimed;

            if (hasReward)
            {
                RefreshRewards(rewards);
            }
        }

        private void RefreshRewards(List<ItemData> rewards)
        {
            _pool ??= new SimpleUIPool<UIRewardQuantity>(rewardPrefab, rewardContainer);

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];

                if (reward.ItemId > 0)
                {
                    var clone = _pool.Get(i);

                    clone.transform.localScale = Vector3.one * rewardScale;

                    clone.Bind(reward.ItemId, reward.Quantity);
                }
            }

            _pool.ReleaseFrom(rewards.Count);
        }
    }
}