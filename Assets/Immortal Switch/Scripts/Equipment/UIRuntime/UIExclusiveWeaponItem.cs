using System;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIExclusiveWeaponItem : UIWeaponItemBase
    {
        [SerializeField] private TMP_Text txtTier;

        private int heroId;
        private Action<int> onClickWithHeroId;

        public void Bind(ExclusiveWeaponCardViewModel vm, Action<int> clickCallback)
        {
            heroId = vm.HeroId;
            onClickWithHeroId = clickCallback;

            if (txtTier != null)
                txtTier.text = "EX";

            string shardText = vm.MaxShard > 0
                ? $"{vm.CurrentShard}/{vm.MaxShard}"
                : vm.CurrentShard.ToString();

            BindCommon(
                vm.Icon,
                $"Lv.{vm.Level}",
                shardText,
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