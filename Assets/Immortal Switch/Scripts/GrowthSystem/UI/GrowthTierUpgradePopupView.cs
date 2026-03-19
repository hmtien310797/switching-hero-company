using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthTierUpgradePopupView : AnimatedUIView
    {
        [Header("Header")]
        [SerializeField] private Image currentTierIcon;
        [SerializeField] private Image nextTierIcon;
        [SerializeField] private TMP_Text currentTierText;
        [SerializeField] private TMP_Text nextTierText;

        [Header("Rows")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private GrowthTierUpgradeRowView rowPrefab;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        private readonly List<GrowthTierUpgradeRowView> rows = new();
        private Action onConfirm;

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnClickConfirm);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            gameObject.SetActive(false);
        }

        public void Show(GrowthTierUpgradePopupData data, Action onConfirmCallback)
        {
            onConfirm = onConfirmCallback;

            if (currentTierIcon != null) currentTierIcon.sprite = data.CurrentTierIcon;
            if (nextTierIcon != null) nextTierIcon.sprite = data.NextTierIcon;

            if (currentTierText != null) currentTierText.text = data.CurrentTier.ToString();
            if (nextTierText != null) nextTierText.text = data.NextTier.ToString();

            EnsureRows(data.Rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                bool active = i < data.Rows.Count;
                rows[i].gameObject.SetActive(active);

                if (active)
                    rows[i].Bind(data.Rows[i]);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            onConfirm = null;
        }

        private void OnClickConfirm()
        {
            onConfirm?.Invoke();
        }

        private void EnsureRows(int count)
        {
            while (rows.Count < count)
            {
                var instance = Instantiate(rowPrefab, contentRoot);
                rows.Add(instance);
            }
        }
    }
}