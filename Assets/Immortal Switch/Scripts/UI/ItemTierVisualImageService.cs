using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Modules;
using Immortal_Switch.Scripts.Modules.Atlas;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class ItemTierVisualImageService : AtlasServiceBase
    {
        public ItemTierVisualImageService(string atlasKey) : base(atlasKey)
        {
        }

        public static Sprite GetItemTierIcon(EItemTier itemTier)
        {
            return GetItemTierIcon(itemTier.ToString());
        }

        public static Sprite GetItemTierFrame(EItemTier itemTier)
        {
            return GetItemTierFrame(itemTier.ToString());
        }

        public static Sprite GetItemTierBg(EItemTier itemTier)
        {
            return GetItemTierBg(itemTier.ToString());
        }

        public static ItemTierEntry GetItemTierEntry(EItemTier itemTier)
        {
            return new ItemTierEntry
            {
                tier = itemTier,
                background = GetItemTierBg(itemTier),
                border = GetItemTierFrame(itemTier),
                tierIcon = GetItemTierIcon(itemTier)
            };
        }

        public static List<ItemTierEntry> GetItemTierEntries()
        {
            List<ItemTierEntry> itemTierEntries = new();
            EItemTier[] values = (EItemTier[])Enum.GetValues(typeof(EItemTier));

            for (int i = 0; i < values.Length; i++)
            {
                EItemTier tier = values[i];

                itemTierEntries.Add(new ItemTierEntry
                {
                    tier = tier,
                    background = GetItemTierBg(tier),
                    border = GetItemTierFrame(tier),
                    tierIcon = GetItemTierIcon(tier)
                });
            }

            return itemTierEntries;
        }

        public static ItemTierEntry GetItemTierEntry(WeaponTier itemTier)
        {
            EItemTier newTier = EItemTier.D;

            switch (itemTier)
            {
                case WeaponTier.A:
                    newTier = EItemTier.A;
                    break;

                case WeaponTier.C:
                    newTier = EItemTier.C;
                    break;

                case WeaponTier.B:
                    newTier = EItemTier.B;
                    break;

                case WeaponTier.S:
                    newTier = EItemTier.S;
                    break;

                case WeaponTier.SS:
                    newTier = EItemTier.SS;
                    break;

                case WeaponTier.D:
                    newTier = EItemTier.D;
                    break;
            }

            return new ItemTierEntry
            {
                tier = newTier,
                background = GetItemTierBg(newTier),
                border = GetItemTierFrame(newTier),
                tierIcon = GetItemTierIcon(newTier)
            };
        }

        public static Sprite GetItemTierIcon(WeaponTier itemTier)
        {
            return GetItemTierIcon(itemTier.ToString());
        }

        public static Sprite GetItemTierFrame(WeaponTier itemTier)
        {
            return GetItemTierFrame(itemTier.ToString());
        }

        public static Sprite GetItemTierBg(WeaponTier itemTier)
        {
            return GetItemTierBg(itemTier.ToString());
        }

        private static Sprite GetItemTierIcon(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"tier_{tier}".ToLower();
            return ModuleManager.ItemTierVisualAtlas.LoadIcon(valueKey);
        }

        private static Sprite GetItemTierBg(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"bg_{tier}".ToLower();
            return ModuleManager.ItemTierVisualAtlas.LoadIcon(valueKey);
        }

        private static Sprite GetItemTierFrame(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return null;

            string valueKey = $"frame_{tier}".ToLower();
            return ModuleManager.ItemTierVisualAtlas.LoadIcon(valueKey);
        }
    }
}