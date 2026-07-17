using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationEquipmentInfoArgs
    {
        /// <summary>
        /// thong tin trang bi
        /// </summary>
        public PlayerEquipViewData EquipView { get; set; }
    }

    public class UITransmutationEquipmentInfoPanel : AnimatedUIView
    {
        [Header("Equip & Unique Effect")]
        [SerializeField]
        private UITransmutationSystemReplaceInfoPanel equipmentInfo;

        // --- Private Field ---
        private UITransmutationEquipmentInfoArgs _args;

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is UITransmutationEquipmentInfoArgs runtime)
            {
                _args = runtime;

                equipmentInfo.Bind(_args.EquipView, null, false);
            }
        }
    }
}