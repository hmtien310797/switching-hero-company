using System.Collections.Generic;
using System.Numerics;
using Immortal_Switch.Scripts.Items.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public static class RewardHelper
    {
        public static List<RewardEntry> ParseRewards(string rewards)
        {
            Debug.Log($"ParseRewards: {rewards}");
            var results = new List<RewardEntry>();
            var splitRewards = rewards.Split(';');

            foreach (var reward in splitRewards)
            {
                var splits = reward.Split(':');

                // có 2 key là item key va quantity
                if (splits.Length > 1)
                {
                    var itemKey = splits[0];
                    BigInteger.TryParse(splits[1], out var quantity);

                    results.Add(new RewardEntry
                    {
                        itemKey = itemKey,
                        quantity = quantity,
                    });
                }
                else
                {
                    Debug.LogError($"Reward {reward} wrong config");
                }
            }

            return results;
        }
    }
}