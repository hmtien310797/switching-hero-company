using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonConfirmPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle skipToggle;

        private Action<bool> confirmAction;

        private void Awake()
        {
            confirmButton?.onClick.AddListener(HandleConfirm);
            cancelButton?.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(string message, bool skipValue, Action<bool> onConfirm)
        {
            confirmAction = onConfirm;

            if (messageText != null)
                messageText.text = message;

            if (skipToggle != null)
                skipToggle.isOn = skipValue;

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
            bool skip = skipToggle != null && skipToggle.isOn;
            confirmAction?.Invoke(skip);
            Hide();
        }
    }
}