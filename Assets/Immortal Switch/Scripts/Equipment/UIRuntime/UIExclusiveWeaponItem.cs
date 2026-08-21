using System;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIExclusiveWeaponItem : UIWeaponItemBase
    {
        private int heroId;
        private Action<int> onClickWithHeroId;

        public void Bind(ExclusiveWeaponCardViewModel vm, Action<int> clickCallback)
        {
            heroId = vm.HeroId;
            onClickWithHeroId = clickCallback;

            BindTierVisual(WeaponTier.SS);

            string shardText = vm.MaxShard > 0
                ? $"{vm.CurrentShard}/{vm.MaxShard}"
                : vm.CurrentShard.ToString();

            BindCommon(
                vm.Icon,
                LocalizationManager.GetText(LocalizationKeys.UI_LV, vm.Level),
                shardText,
                vm.ShardProgressNormalized,
                vm.MaxShard > 0,
                $"{vm.CurrentStar}/{vm.MaxStar}",
                vm.IsEquipped,
                !vm.IsUnlocked,
                vm.CanLevelUp || vm.CanLimitBreak || vm.CanTranscend,
                vm.IsSelected,
                () => onClickWithHeroId?.Invoke(heroId)
            );
        }
    }
}