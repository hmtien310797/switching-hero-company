using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongSevenDayRewardPanel : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtClaim;

        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform instantRewardContainer;

        [SerializeField]
        private RectTransform bonusRewardContainer;

        [SerializeField]
        private UIItemSlot rewardPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIItemSlot> _instantPools;
        private SimpleUIPool<UIItemSlot> _bonusPools;

        private Action<int> _onClickClaim;
        private int _currentDay;

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
            _onClickClaim?.Invoke(_currentDay);
        }

        public void Bind(
            List<ItemData> instantRewards,
            List<ItemData> bonusRewards,
            Action<int> onClickClaim,
            bool isClaimedFree,
            bool isBonusPurchased,
            bool isClaimedPack,
            string packPrice,
            int currentDay
        )
        {
            if (!isClaimedFree)
            {
                txtClaim.text = "Nhận ngay";
                btnClaim.interactable = true;
            }
            else if (!isBonusPurchased)
            {
                txtClaim.text = packPrice;
                btnClaim.interactable = true;
            }
            else if (!isClaimedPack)
            {
                txtClaim.text = "Nhận ngay";
                btnClaim.interactable = true;
            }
            else
            {
                txtClaim.text = "Đã nhận";
                btnClaim.interactable = false;
            }

            txtTitle.text = $"Ngày {currentDay}";

            _currentDay = currentDay;
            _onClickClaim = onClickClaim;

            _instantPools ??= new SimpleUIPool<UIItemSlot>(rewardPrefab, instantRewardContainer);
            _bonusPools ??= new SimpleUIPool<UIItemSlot>(rewardPrefab, bonusRewardContainer);

            RefreshReward(_instantPools, instantRewards);
            RefreshReward(_bonusPools, bonusRewards);
        }

        private void RefreshReward(SimpleUIPool<UIItemSlot> pools, List<ItemData> rewards)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                var clone = pools.Get(i);
                clone.Bind(rewards[i].ItemId, true);
            }

            pools.ReleaseFrom(rewards.Count);
        }
    }
}