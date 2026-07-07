using System.Collections.Generic;
using System.Numerics;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.PlayerSystem.Models;

namespace Immortal_Switch.Scripts.TransmutationSystem.Models
{
    public class TransmutationSystemData
    {
        /// <summary>
        /// ds equip
        /// key: item type
        /// value: thong tin equip.
        /// </summary>
        public Dictionary<string, PlayerEquipItem> Equips = new();

        /// <summary>
        /// cau hinh cai dat setting
        /// </summary>
        public TransmutationSystemAutoSettingData Setting = new();

        /// <summary>
        /// trang bị lay ra nhưng chưa dismantle. / equip 
        /// </summary>
        public PlayerEquipItem StuckEquip;

        /// <summary>
        /// exp
        /// </summary>
        public BigInteger Exp;

        /// <summary>
        /// energy
        /// </summary>
        public BigInteger Energy;

        /// <summary>
        /// level hien tai
        /// </summary>
        public int Level;

        public void UpdateEnergy(BigInteger energy)
        {
            Energy = energy;
        }

        public void UpdateExp(BigInteger exp)
        {
            Exp = exp;
        }

        public void UpdateLevel(int level)
        {
            Level = level;
        }
    }

    public class TransmutationSystemAutoSettingData
    {
        /// <summary>
        /// luu cau hinh unique
        /// </summary>
        public List<List<string>> UniqueOptions = new();

        /// <summary>
        /// tier da chon
        /// </summary>
        public EItemTier Tier;

        /// <summary>
        /// sl chon
        /// </summary>
        public int Count;

        /// <summary>
        /// cho nguyen lieu
        /// </summary>
        public bool IsWaiting;

        /// <summary>
        /// co bat cai dạt hay ko
        /// </summary>
        public bool Enabled;
    }

    public class TransmutationSystemTotalStatData
    {
        /// <summary>
        /// du lieu entries
        /// </summary>
        public List<TransmutationSystemTotalStatEntry> Entries;
    }

    public struct TransmutationSystemTotalStatEntry
    {
        /// <summary>
        /// ten cua stat
        /// </summary>
        public string Title;

        /// <summary>
        /// gia tri cua stat
        /// </summary>
        public BigInteger Value;

        /// <summary>
        /// co phai hieu ung dac biet khong
        /// </summary>
        public bool IsUnique;
    }
}