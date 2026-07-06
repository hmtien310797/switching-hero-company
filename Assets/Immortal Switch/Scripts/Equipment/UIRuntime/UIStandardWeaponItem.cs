using System;
using Immortal_Switch.Scripts.Equipment.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIStandardWeaponItem : UIWeaponItemBase
    {
        private int weaponId;
        private Action<int> onClickWithId;

        public void Bind(StandardWeaponCardViewModel vm, Action<int> clickCallback)
        {
            weaponId = vm.WeaponId;
            onClickWithId = clickCallback;

            BindTierVisual(vm.Tier);

            if (starDisplay != null)
                starDisplay.BindStandard(vm.Star);

            string shardText = vm.MaxShard > 0
                ? $"{vm.CurrentShard}/{vm.MaxShard}"
                : vm.CurrentShard.ToString();

            BindCommon(
                vm.Icon,
                $"Lv.{vm.Level}",
                shardText,
                vm.ShardProgressNormalized,
                vm.MaxShard > 0,
                string.Empty,
                vm.IsEquipped,
                !vm.IsUnlocked,
                vm.CanFuse || vm.CanLevelUp || vm.CanLimitBreak,
                vm.IsSelected,
                () => onClickWithId?.Invoke(weaponId)
            );
        }
    }
}