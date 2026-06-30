using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradePanelView : MonoBehaviour
    {
        [Header("Progress")] [SerializeField] private TextMeshProUGUI txtProgress;
        [SerializeField] private Image imgSlider;

        [SerializeField] private TMP_Text goldText;
        [SerializeField] private GrowthUpgradeAmountSelector amountSelector;

        [SerializeField] private Transform contentRoot;
        [SerializeField] private StatTierView prefab;

        private readonly List<StatTierView> views = new();

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
        }

        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 29:
                case 30:
                case 31:
                    views[0].OnClickLevelUp();
                    break;
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 29:
                case 30:
                case 31:
                    return views[0].levelUpButton.transform as RectTransform;

                default:
                    return null;
            }
        }

        public void Bind(
            List<StatTierViewData> datas,
            BigNumber gold,
            int selectedAmount,
            Action<StatType> onUpgrade,
            Action<int> onAmountChanged)
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
            
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            
            goldText.text = gold.ToString();

            amountSelector.Initialize(selectedAmount, onAmountChanged);

            Ensure(datas.Count);

            for (int i = 0; i < views.Count; i++)
            {
                bool active = i < datas.Count;
                views[i].gameObject.SetActive(active);

                if (active)
                    views[i].Initialize(datas[i], onUpgrade);
            }

            var sum = datas.Sum(v => v.StatMaxStack);
            var cur = datas.Sum(v => v.StatCurrentStack);
            var progress = cur / (sum * 1f);
            txtProgress.text = $"{Mathf.RoundToInt(progress * 100)}%";
            imgSlider.fillAmount = Mathf.Clamp01(progress);
        }

        private void Ensure(int count)
        {
            while (views.Count < count)
            {
                var v = Instantiate(prefab, contentRoot);
                views.Add(v);
            }
        }
    }
}