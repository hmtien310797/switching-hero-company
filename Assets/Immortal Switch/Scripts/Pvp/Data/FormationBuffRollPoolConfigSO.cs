using System;
using Immortal_Switch.Scripts.Pvp.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Data
{
    /// <summary>
    /// Roll pool config (DOCX §25 — FormationBuffRollPoolConfigSO). PoolId, cost, currency,
    /// rarity rates, included buffs, pity, duplicate. <b>FLAGGED:</b> editor validation phải đảm bảo
    /// enabled rates total 100% + không reference missing BuffId (DOCX §25).
    /// </summary>
    [CreateAssetMenu(menuName = "PvP/Formation Buff Roll Pool")]
    public sealed class FormationBuffRollPoolConfigSO : ScriptableObject
    {
        public string PoolId = "standard";
        public int RollCost = 500;
        public PvpCurrencyType CurrencyType = PvpCurrencyType.ArenaToken;

        public FormationBuffRarityRate[] RarityRates;
        public string[] IncludedBuffIds;

        public FormationBuffPityConfig Pity = new();
        public FormationBuffDuplicateConfig Duplicate = new();
    }
}
