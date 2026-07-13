using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Items.Models;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopGloryItem : MonoBehaviour
    {
        [Header("Main references")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [Header("Change references")]
        [SerializeField]
        private Image imgBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprUnclaimedBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprClaimedBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprUnclaimedBtn;

        [PreviewField]
        [SerializeField]
        private Sprite sprClaimBtn;

        [Header("Progress references")]
        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private Image imgFill;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIShopProductItem rewardPrefab;

        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtClaim;

        [SerializeField]
        private GameObject goClaimed;

        // --- Private Fields ---
        private List<UIShopProductItem> _rewards = new();
        private Action<bool, int> _onClickClaim;

        private int _shopPackId;
        private bool _canClaim;

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
            _onClickClaim?.Invoke(_canClaim, _shopPackId);
        }

        public void Bind(
            int currentValue, int limit,
            bool isClaimed, string title, int shopPackId,
            IReadOnlyList<ItemRewardData> rewards,
            Action<bool, int> onClickClaim
        )
        {
            txtTitle.text = title;
            txtProgress.text = $"{Math.Min(currentValue, limit)}/{limit}";
            imgFill.fillAmount = currentValue / (limit * 1f);

            _shopPackId = shopPackId;
            _onClickClaim = onClickClaim;

            if (!isClaimed)
            {
                _canClaim = currentValue >= limit;
                imgBg.sprite = sprUnclaimedBg;

                if (_canClaim)
                {
                    btnClaim.image.sprite = sprClaimBtn;
                    txtClaim.text = "Nhận";
                }
                else
                {
                    btnClaim.image.sprite = sprUnclaimedBtn;
                    txtClaim.text = "Nạp";
                }

                goClaimed.SetActive(false);
            }
            else
            {
                _canClaim = false;
                imgBg.sprite = sprClaimedBg;

                btnClaim.gameObject.SetActive(false);
                goClaimed.SetActive(true);
            }

            RefreshRewards(rewards);
        }

        private void RefreshRewards(IReadOnlyList<ItemRewardData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];

                if (_rewards.Count > i)
                {
                    var clone = _rewards[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                }
                else
                {
                    var clone = Instantiate(rewardPrefab, rewardContainer);
                    clone.Bind(reward.ItemIcon, reward.Quantity);
                    _rewards.Add(clone);
                }
            }
        }
    }
}