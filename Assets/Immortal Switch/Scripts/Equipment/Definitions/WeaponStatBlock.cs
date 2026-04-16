using System;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [Serializable]
    public struct WeaponStatBlock
    {
        public StatType StatType;
        public ModifierOp Operation;
        public float Value;
    }
}