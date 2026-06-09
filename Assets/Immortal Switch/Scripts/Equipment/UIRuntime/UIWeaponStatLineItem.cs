using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponStatLineItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtValue;

        public void Bind(WeaponStatLineViewModel vm)
        {
            if (txtName != null)
                txtName.text = vm.DisplayName;

            /*if (txtValue != null)
                txtValue.text = vm.DisplayValue;*/
            if (txtValue != null)
                txtValue.text="0";
        }
    }
}