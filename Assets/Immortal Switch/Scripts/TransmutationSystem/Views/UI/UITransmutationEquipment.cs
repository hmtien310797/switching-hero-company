using Immortal_Switch.Scripts.Shared.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationEquipment : MonoBehaviour
    {
        [Header("Item view")] [SerializeField] private Image imgTier;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgBorder;
        [SerializeField] private Image imgBg;
        [SerializeField] private TMP_Text txtLevel;
        [SerializeField] private GameObject goEmpty;

        public void SetEmpty(bool value)
        {
            if (goEmpty != null)
            {
                goEmpty.SetActive(value);
            }
        }

        public void Bind(EquipmentTierEntry cfg, int level)
        {
            SetEmpty(false);
            txtLevel.text = $"Lv {level:00}";
            imgBg.sprite = cfg.background;
            imgBorder.sprite = cfg.border;
            imgTier.sprite = cfg.tier;
        }
    }
}