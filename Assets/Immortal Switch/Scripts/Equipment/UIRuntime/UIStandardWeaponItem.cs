using System;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIStandardWeaponItem : UIWeaponItemBase
    {
        [SerializeField] private TMP_Text txtTier;

        private int weaponId;
        private Action<int> onClickWithId;

        public void Bind(StandardWeaponCardViewModel vm, Action<int> clickCallback)
        {
            weaponId = vm.WeaponId;
            onClickWithId = clickCallback;

            if (txtTier != null)
                txtTier.text = $"{vm.Tier}";

            string shardText = vm.MaxShard > 0
                ? $"{vm.CurrentShard}/{vm.MaxShard}"
                : vm.CurrentShard.ToString();

            BindCommon(
                vm.Icon,
                $"Lv.{vm.Level}",
                shardText,
                vm.ShardProgressNormalized,
                vm.MaxShard > 0,
                $"{vm.Star}",
                vm.IsEquipped,
                !vm.IsUnlocked,
                vm.CanFuse || vm.CanLevelUp || vm.CanLimitBreak,
                vm.IsSelected,
                () => onClickWithId?.Invoke(weaponId)
            );
        }
    }
}