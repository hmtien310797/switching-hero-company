using System;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Kết quả resolve 1 roll (DOCX §22 — FormationBuffRollResolveResult). GrantedNew = true nếu
    /// buff mới được unlock; IsDuplicate = true + ShardsGained nếu duplicate → shard.
    /// </summary>
    [Serializable]
    public sealed class FormationBuffRollResolveResult
    {
        public string RollId;
        public string SelectedBuffId;
        public BuffRarity SelectedRarity;
        public bool GrantedNew;
        public bool IsDuplicate;
        public int ShardsGained;
        public string Message;
    }
}
