using System;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.UI
{
    public class SummonConfirmPopup : MonoBehaviour
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
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Hide);
            }

            // hide chay sau khi bam button.
            Hide();
        }

        public void Show(int gemCost, Action onConfirm)
        {
            confirmAction = onConfirm;

            if (messageText != null)
            {
                messageText.text = $"Không đủ Vé Anh hùng.\nLần triệu hồi này sẽ tiêu tốn {gemCost} Kim cương.\nXác nhận?";
            }

            if (skipToggle != null)
            {
                skipToggle.isOn = HeroSummonManager.Instance != null &&
                                  HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
            }

            if (root != null)
            {
                root.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleConfirm()
        {
            if (HeroSummonManager.Instance != null && skipToggle != null)
            {
                HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm = skipToggle.isOn;
                HeroSummonManager.Instance.Save();
            }

            confirmAction?.Invoke();
            Hide();
        }
    }
}