using System.Collections.Generic;
using System.Numerics;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.StatSystem;

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
}