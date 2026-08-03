using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    [Serializable]
    public sealed class TransmutationEquipSnapshot
    {
        public string ItemType;
        public string CfgId;
        public string Tier;
        public int Level;
    }

    /// <summary>
    /// Transmutation fragment (DOCX §9). <b>FLAGGED:</b> inferred từ TransmutationModels.
    /// Exp/Crystal là BigInteger → lưu string để JSON-friendly.
    /// </summary>
    [Serializable]
    public sealed class TransmutationSnapshot
    {
        public int Level;
        public string Exp;
        public string Crystal;
        public List<TransmutationEquipSnapshot> Equips = new();
    }
}
