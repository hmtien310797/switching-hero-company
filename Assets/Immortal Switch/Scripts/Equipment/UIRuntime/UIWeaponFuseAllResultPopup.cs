using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponFuseAllResultPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text txtTitle;
        [SerializeField] private TMP_Text txtTapToClose;

        [Header("Reward List")]
        [SerializeField] private Transform rewardContainer;
        [SerializeField] private UIWeaponFuseAllRewardItem rewardItemPrefab;

        private readonly List<UIWeaponFuseAllRewardItem> rewardItems = new();

        private void Awake()
        {
            BindClose();
            Hide();
        }

        private void BindClose()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
        }

        public void Show(WeaponFuseAllResult result)
        {
            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            if (txtTitle != null)
                txtTitle.text = "Items Obtained";

            if (txtTapToClose != null)
                txtTapToClose.text = "Tap to close the window";

            var rewards = result != null ? result.Rewards : null;
            int count = rewards != null ? rewards.Count : 0;

            EnsureRewardPool(count);

            for (int i = 0; i < rewardItems.Count; i++)
            {
                bool active = i < count;
                rewardItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                rewardItems[i].Bind(rewards[i]);
            }
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void EnsureRewardPool(int targetCount)
        {
            while (rewardItems.Count < targetCount)
            {
                var item = Instantiate(rewardItemPrefab, rewardContainer);
                rewardItems.Add(item);
            }
        }
    }
}