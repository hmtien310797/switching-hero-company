using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Models;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionRewardGroupEntry : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Button btnClaim;

        [SerializeField] private Button btnAdsX2;

        [SerializeField] private TextMeshProUGUI txtQuantity;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgTier;
        [SerializeField] private GameObject goOverlayClaimed;

        // --- Private Fields ---
        public DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow Row { get; private set; }

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
            btnAdsX2.onClick.AddListener(OnClickAdsX2);
            btnAdsX2.gameObject.SetActive(false);
        }

        private void OnClickAdsX2()
        {
            if (Row != null)
            {
                MissionSystemManager.Instance.RewardGroupClaim(Row, true);
            }
        }

        private void OnClickClaim()
        {
            if (Row != null)
            {
                MissionSystemManager.Instance.RewardGroupClaim(Row, false);
            }
        }

        public void BindState([CanBeNull] MissionSystemPoint state)
        {
            if (state != null)
            {
                btnAdsX2.gameObject.SetActive(!state.X2Claimed);
                goOverlayClaimed.SetActive(true);
            }
            else
            {
                goOverlayClaimed.SetActive(false);
                btnAdsX2.gameObject.SetActive(false);
            }
        }

        public void Bind(DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow row)
        {
            Row = row;
            txtQuantity.text = row.pointThreshold.ToString();
        }
    }
}