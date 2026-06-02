using Immortal_Switch.Scripts.Equipment.UIRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.PlayerSystem.UI
{
    public class UIProfileRenamePopup : BaseUIPopup
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtPrice;

        [SerializeField] private Button btnConfirm;

        private void Awake()
        {
            BindButtons();
        }

        protected override void BindButtons()
        {
            base.BindButtons();
            btnConfirm.onClick.AddListener(OnConfirm);
        }

        private void OnConfirm()
        {
        }
    }
}