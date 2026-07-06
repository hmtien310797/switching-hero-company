using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponUpgradeStatLineItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtStatName;
        [SerializeField] private TMP_Text txtCurrentValue;
        [SerializeField] private TMP_Text txtNextValue;

        public void Bind(string statName, string currentValue, string nextValue)
        {
            if (txtStatName != null)
                txtStatName.text = statName;

            if (txtCurrentValue != null)
                txtCurrentValue.text = currentValue;

            if (txtNextValue != null)
                txtNextValue.text = nextValue;
        }
    }
}