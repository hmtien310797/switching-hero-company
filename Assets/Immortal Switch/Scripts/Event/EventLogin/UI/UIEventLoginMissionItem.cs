using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shop.Views.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLogin.UI
{
    public class UIEventLoginMissionItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtNumber;

        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private Button btnGo;

        [SerializeField]
        private GameObject goClaimed;

        [Header("Info references")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtDesc;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private Image imgFill;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIShopProductItem rewardPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIShopProductItem> _pools;
        private System.Action<EventLoginMissionDto> _onClaim;
        private EventLoginMissionDto _mission;

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
            if (_mission != null)
            {
                _onClaim?.Invoke(_mission);
            }
        }

        public void Bind(
            int number,
            EventLoginMissionDto mission,
            bool isUnlockedDay,
            System.Action<EventLoginMissionDto> onClaim)
        {
            txtNumber.text = $"{number}";
            txtTitle.text = LocalizationManager.GetText(mission.TitleVi);
            txtDesc.text = LocalizationManager.GetText(mission.Trigger);
            txtProgress.text = $"{mission.Progress}/{mission.Target}";
            imgFill.fillAmount = mission.Target > 0 ? mission.Progress / (float)mission.Target : 0f;

            _mission = mission;
            _onClaim = onClaim;

            if (isUnlockedDay)
            {
                if (mission.Progress < mission.Target)
                {
                    btnClaim.interactable = false;
                    btnGo.interactable = true;

                    btnClaim.gameObject.SetActive(false);
                    btnGo.gameObject.SetActive(false);
                    goClaimed.SetActive(true);
                }
                else if (mission.IsClaimed)
                {
                    btnClaim.interactable = false;
                    btnGo.interactable = false;

                    btnClaim.gameObject.SetActive(false);
                    btnGo.gameObject.SetActive(false);
                    goClaimed.SetActive(true);
                }
                else
                {
                    btnClaim.interactable = true;
                    btnGo.interactable = false;

                    btnClaim.gameObject.SetActive(true);
                    btnGo.gameObject.SetActive(false);
                    goClaimed.SetActive(false);
                }
            }
            else
            {
                btnClaim.gameObject.SetActive(false);
                btnGo.gameObject.SetActive(false);
                goClaimed.SetActive(false);
            }

            RefreshRewards(new List<ItemData>
            {
                new(mission.Reward.ItemId, mission.Reward.Amount),
            });
        }

        private void RefreshRewards(List<ItemData> rewards)
        {
            _pools ??= new SimpleUIPool<UIShopProductItem>(rewardPrefab, rewardContainer);

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var clone = _pools.Get(i);

                clone.Bind(reward.ItemId, reward.Quantity);
            }

            _pools.ReleaseFrom(rewards.Count);
        }
    }
}