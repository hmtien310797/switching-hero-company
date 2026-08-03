using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Config duplicate conversion (DOCX §21 — duplicate → shards). Shard value per rarity.
    /// </summary>
    [Serializable]
    public sealed class FormationBuffDuplicateConfig
    {
        public Dictionary<BuffRarity, int> ShardValueByRarity = new()
        {
            { BuffRarity.Common, 5 },
            { BuffRarity.Uncommon, 10 },
            { BuffRarity.Rare, 20 },
            { BuffRarity.Epic, 40 },
            { BuffRarity.Legendary, 100 }
        };

        public int GetShardValue(BuffRarity rarity)
        {
            return ShardValueByRarity != null && ShardValueByRarity.TryGetValue(rarity, out var v) ? v : 0;
        }
    }
}
