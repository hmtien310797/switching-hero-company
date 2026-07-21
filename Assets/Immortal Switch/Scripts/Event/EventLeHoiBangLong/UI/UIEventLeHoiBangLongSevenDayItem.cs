using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongSevenDayItem : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtDay;

        [SerializeField]
        private GameObject goOverlayClaimed;

        [Header("Reward references")]
        [SerializeField]
        private UIItemSlot rewardSlot;

        [Header("Bg references")]
        [SerializeField]
        private Image imgBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprBgNormal;

        [PreviewField]
        [SerializeField]
        private Sprite sprBgSpecial;

        // --- Private Fields ---
        private SimpleUIPool<UIItemSlot> _pools;
        private DynamicHeroesGlobalSpecificationsEventBLCheckInRow _row;

        private Action<int> _onClickClaim;

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
            if (_row != null)
            {
                _onClickClaim?.Invoke(_row.day);
            }
        }

        public void Bind(
            DynamicHeroesGlobalSpecificationsEventBLCheckInRow row,
            Action<int> onClickClaim,
            int currentDay,
            bool isClaimed
        )
        {
            _row = row;
            _onClickClaim = onClickClaim;

            goOverlayClaimed.SetActive(isClaimed);

            txtDay.text = $"Ngày {row.day}";
            btnClaim.interactable = row.day <= currentDay && !isClaimed;
            imgBg.sprite = row.day == 7 ? sprBgSpecial : sprBgNormal;

            rewardSlot.Bind(row.rewardId);
        }
    }
}