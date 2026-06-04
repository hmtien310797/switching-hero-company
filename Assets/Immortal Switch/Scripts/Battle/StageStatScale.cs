using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [Serializable]
    public struct StageStatScale
    {
        public float HpMultiplier;
        public float AtkMultiplier;
        public float DefMultiplier;

        public static StageStatScale Identity => new StageStatScale
        {
            HpMultiplier = 1f,
            AtkMultiplier = 1f,
            DefMultiplier = 1f
        };

        public void Normalize()
        {
            HpMultiplier = Mathf.Max(0.01f, HpMultiplier);
            AtkMultiplier = Mathf.Max(0.01f, AtkMultiplier);
            DefMultiplier = Mathf.Max(0.01f, DefMultiplier);
        }
    }
}