using System;
using Immortal_Switch.Scripts.SkillSummon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonConfirmPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle skipToggle;

        private Action confirmAction;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Hide);

            Hide();
        }

        public void Show(int gemCost, Action onConfirm)
        {
            confirmAction = onConfirm;

            if (messageText != null)
                messageText.text = $"Not enough Skill Tickets.\nThis summon will cost {gemCost} Diamonds.\nConfirm?";

            if (skipToggle != null)
                skipToggle.isOn = SkillSummonManager.Instance != null &&
                                  SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm;

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void HandleConfirm()
        {
            if (SkillSummonManager.Instance != null && skipToggle != null)
            {
                SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm = skipToggle.isOn;
                SkillSummonManager.Instance.Save();
            }

            confirmAction?.Invoke();
            Hide();
        }
    }
}