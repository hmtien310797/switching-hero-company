using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongMissionItem : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtClaim;

        [SerializeField]
        private TextMeshProUGUI txtDay;

        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private Image imgFill;

        [SerializeField]
        private GameObject goLine;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIItemSlot rewardPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIItemSlot> _pools;
        private DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow _row;
        private Action<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> _onClaim;

        private bool _hasCanJump;
        private bool _hasCanClaim;

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
            if (_hasCanJump)
            {
                // todo: jump tới ui cần
            }
            else if (_hasCanClaim)
            {
                _onClaim?.Invoke(_row);
            }
        }

        public void Bind(
            DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row,
            int currentValue,
            bool isClaimed,
            Action<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> onClaim,
            bool isLast
        )
        {
            _row = row;
            _onClaim = onClaim;

            txtTitle.text = row.titleVi;
            txtDay.text = row.sortOrder.ToString();
            txtProgress.text = $"Tiến độ: {currentValue}/{row.target}";
            imgFill.fillAmount = row.target <= 0 ? 0f : currentValue / (row.target * 1f);

            if (isClaimed)
            {
                _hasCanClaim = false;
                _hasCanJump = false;

                btnClaim.gameObject.SetActive(false);
            }
            else
            {
                _hasCanJump = currentValue < row.target;
                _hasCanClaim = !_hasCanJump;
                txtClaim.text = _hasCanClaim ? "Nhận ngay" : "Đến";

                btnClaim.gameObject.SetActive(true);
            }

            goLine.SetActive(!isLast);

            RefreshReward(new List<ItemData>
            {
                new(row.itemId, row.quantity),
            });
        }

        private void RefreshReward(List<ItemData> rewards)
        {
            _pools ??= new SimpleUIPool<UIItemSlot>(rewardPrefab, rewardContainer);

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var clone = _pools.Get(i);

                clone.Bind(reward.ItemId);
            }

            _pools.ReleaseFrom(rewards.Count);
        }
    }
}