using System;
using Battle;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponClassTabItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private GameObject deployedStandardMark;
        [SerializeField] private TMP_Text txtE;

        private HeroClass boundClass;
        private Action<HeroClass> onClick;

        public void Bind(WeaponClassTabViewModel vm, Action<HeroClass> clickCallback)
        {
            boundClass = vm.HeroClass;
            onClick = clickCallback;

            if (selectedHighlight != null)
                selectedHighlight.SetActive(vm.IsSelected);

            if (deployedStandardMark != null)
                deployedStandardMark.SetActive(vm.HasDeployedHeroUsingStandard);

            if (txtE != null)
                txtE.gameObject.SetActive(vm.HasDeployedHeroUsingStandard);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onClick?.Invoke(boundClass);
        }
    }
}